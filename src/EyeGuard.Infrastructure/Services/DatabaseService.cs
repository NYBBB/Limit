using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using EyeGuard.Core.Entities;
using SQLite;

namespace EyeGuard.Infrastructure.Services;

public class DatabaseService
{
    private readonly string _dbPath;
    private SQLiteAsyncConnection _database;

    public DatabaseService()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EyeGuard");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        _dbPath = Path.Combine(folder, "data.db");
    }

    private async Task InitAsync()
    {
        if (_database != null) return;

        _database = new SQLiteAsyncConnection(_dbPath);
        await _database.CreateTableAsync<UsageRecord>();
        await _database.CreateTableAsync<FatigueSnapshot>();
        await _database.CreateTableAsync<HourlyUsageRecord>();
        await _database.CreateTableAsync<BreakTaskRecord>();
        await _database.CreateTableAsync<Cluster>();  // Limit 3.0: 工作流簇表
        await _database.CreateTableAsync<DailyAggregateRecord>();  // Phase 6: 每日聚合表
    }

    /// <summary>
    /// 公开初始化方法（供 ClusterService 调用）
    /// </summary>
    public async Task InitializeAsync()
    {
        await InitAsync();
    }

    public async Task<List<UsageRecord>> GetUsageForDateAsync(DateTime date)
    {
        await InitAsync();
        var targetDate = date.Date;
        // Simple approximation, better to use ticks or string format for exact date matching in SQLite if needed, 
        // but for now DateTime works with sqlite-net-pcl if configured or default ticks.
        // Actually sqlite-net-pcl stores DateTime as Ticks by default.

        // We want records where Date == targetDate (ignoring time component if we query by day)
        // Since we store Date as Date component only in the entity property 'Date', 
        // we can query directly.

        return await _database.Table<UsageRecord>().Where(i => i.Date == targetDate).ToListAsync();
    }

    public async Task UpdateUsageAsync(string appName, string appPath, int secondsToAdd)
    {
        await UpdateUsageWithWebsiteAsync(appName, appPath, null, null, secondsToAdd);
    }

    /// <summary>
    /// 更新使用记录（支持网站信息）
    /// </summary>
    public async Task UpdateUsageWithWebsiteAsync(string appName, string appPath, string? websiteName, string? pageTitle, int secondsToAdd)
    {
        await InitAsync();
        var today = DateTime.Today;

        // 构建查询键：如果有网站名，按网站分组；否则按应用分组
        var query = _database.Table<UsageRecord>().Where(i => i.Date == today && i.AppName == appName);

        if (!string.IsNullOrEmpty(websiteName))
        {
            query = query.Where(i => i.WebsiteName == websiteName);
        }
        else if (!string.IsNullOrEmpty(pageTitle))
        {
            query = query.Where(i => i.PageTitle == pageTitle);
        }
        else
        {
            // 普通应用：没有网站信息
            query = query.Where(i => i.WebsiteName == null && i.PageTitle == null);
        }

        var record = await query.FirstOrDefaultAsync();

        if (record == null)
        {
            record = new UsageRecord
            {
                Date = today,
                AppName = appName,
                AppPath = appPath,
                WebsiteName = websiteName,
                PageTitle = pageTitle,
                DurationSeconds = secondsToAdd,
                LastActiveTime = DateTime.Now
            };
            await _database.InsertAsync(record);
        }
        else
        {
            record.DurationSeconds += secondsToAdd;
            record.LastActiveTime = DateTime.Now;
            await _database.UpdateAsync(record);
        }
    }

    public async Task<List<UsageRecord>> GetTopUsageAsync(DateTime date, int count = 5)
    {
        await InitAsync();
        var records = await GetUsageForDateAsync(date);
        return records.OrderByDescending(x => x.DurationSeconds).Take(count).ToList();
    }

    // ===== 疲劳快照相关方法 =====

    /// <summary>
    /// 保存疲劳快照
    /// </summary>
    public async Task SaveFatigueSnapshotAsync(double fatigueValue)
    {
        await InitAsync();
        var now = DateTime.Now;
        var snapshot = new FatigueSnapshot
        {
            Date = now.Date,
            FatigueValue = fatigueValue,
            RecordedAt = now
        };
        await _database.InsertAsync(snapshot);
        System.Diagnostics.Debug.WriteLine($"[DatabaseService] 保存快照成功: Date={snapshot.Date:yyyy-MM-dd}, RecordedAt={snapshot.RecordedAt:HH:mm:ss}, Value={fatigueValue:F2}%");
    }

    /// <summary>
    /// 获取指定日期的所有疲劳快照
    /// </summary>
    public async Task<List<FatigueSnapshot>> GetFatigueSnapshotsAsync(DateTime date)
    {
        await InitAsync();
        var targetDate = date.Date;
        System.Diagnostics.Debug.WriteLine($"[DatabaseService] 查询快照: 目标日期={targetDate:yyyy-MM-dd}");

        var snapshots = await _database.Table<FatigueSnapshot>()
            .Where(s => s.Date == targetDate)
            .OrderBy(s => s.RecordedAt)
            .ToListAsync();

        System.Diagnostics.Debug.WriteLine($"[DatabaseService] 查询结果: 找到 {snapshots.Count} 条快照");
        if (snapshots.Count > 0)
        {
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] 第一条: Date={snapshots[0].Date:yyyy-MM-dd}, RecordedAt={snapshots[0].RecordedAt:HH:mm:ss}");
        }

        return snapshots;
    }

    /// <summary>
    /// 获取最新的疲劳快照（用于启动时恢复）
    /// </summary>
    public async Task<FatigueSnapshot?> GetLatestFatigueSnapshotAsync()
    {
        await InitAsync();
        return await _database.Table<FatigueSnapshot>()
            .OrderByDescending(s => s.RecordedAt)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// 清除指定日期之前的疲劳快照（数据清理）
    /// </summary>
    public async Task DeleteOldFatigueSnapshotsAsync(DateTime beforeDate)
    {
        await InitAsync();
        await _database.ExecuteAsync(
            "DELETE FROM FatigueSnapshots WHERE Date < ?",
            beforeDate.Date);
    }

    // ===== 每小时使用记录相关方法 =====

    /// <summary>
    /// 更新每小时使用记录（当前小时的应用使用时长）
    /// </summary>
    public async Task UpdateHourlyUsageAsync(string appName, int secondsToAdd)
    {
        await InitAsync();
        var now = DateTime.Now;
        var today = now.Date;
        var currentHour = now.Hour;

        // 查找当前小时的记录
        var record = await _database.Table<HourlyUsageRecord>()
            .Where(r => r.Date == today && r.Hour == currentHour && r.AppName == appName)
            .FirstOrDefaultAsync();

        if (record == null)
        {
            record = new HourlyUsageRecord
            {
                Date = today,
                Hour = currentHour,
                AppName = appName,
                DurationSeconds = secondsToAdd,
                UpdatedAt = now
            };
            await _database.InsertAsync(record);
        }
        else
        {
            record.DurationSeconds += secondsToAdd;
            record.UpdatedAt = now;
            await _database.UpdateAsync(record);
        }
    }

    /// <summary>
    /// 获取指定日期的每小时使用记录
    /// </summary>
    public async Task<List<HourlyUsageRecord>> GetHourlyUsageAsync(DateTime date)
    {
        await InitAsync();
        return await _database.Table<HourlyUsageRecord>()
            .Where(r => r.Date == date.Date)
            .OrderBy(r => r.Hour)
            .ToListAsync();
    }

    /// <summary>
    /// 获取指定日期某小时的 Top N 应用
    /// </summary>
    public async Task<List<HourlyUsageRecord>> GetTopAppsForHourAsync(DateTime date, int hour, int count = 5)
    {
        await InitAsync();
        return await _database.Table<HourlyUsageRecord>()
            .Where(r => r.Date == date.Date && r.Hour == hour)
            .OrderByDescending(r => r.DurationSeconds)
            .Take(count)
            .ToListAsync();
    }

    // ===== Limit 3.0: 工作流簇相关方法 =====

    /// <summary>
    /// 获取所有簇
    /// </summary>
    public async Task<List<Cluster>> GetAllClustersAsync()
    {
        await InitAsync();
        return await _database.Table<Cluster>().ToListAsync();
    }

    /// <summary>
    /// 保存簇（新增或更新）
    /// </summary>
    public async Task SaveClusterAsync(Cluster cluster)
    {
        await InitAsync();
        if (cluster.Id == 0)
        {
            await _database.InsertAsync(cluster);
        }
        else
        {
            await _database.UpdateAsync(cluster);
        }
    }

    /// <summary>
    /// 删除簇
    /// </summary>
    public async Task DeleteClusterAsync(int clusterId)
    {
        await InitAsync();
        await _database.DeleteAsync<Cluster>(clusterId);
    }

    // ===== Phase 6: 数据聚合相关方法 =====

    /// <summary>
    /// 获取所有每小时使用记录
    /// </summary>
    public async Task<List<HourlyUsageRecord>> GetAllHourlyUsageAsync()
    {
        await InitAsync();
        return await _database.Table<HourlyUsageRecord>()
            .OrderBy(r => r.Date)
            .ThenBy(r => r.Hour)
            .ToListAsync();
    }

    /// <summary>
    /// 保存每日聚合记录
    /// </summary>
    public async Task SaveDailyAggregateAsync(DailyAggregateRecord record)
    {
        await InitAsync();

        // 检查是否已存在该日期的聚合记录
        var existing = await _database.Table<DailyAggregateRecord>()
            .Where(r => r.Date == record.Date)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            record.Id = existing.Id;
            await _database.UpdateAsync(record);
        }
        else
        {
            await _database.InsertAsync(record);
        }
    }

    /// <summary>
    /// 删除指定日期的每小时使用记录
    /// </summary>
    public async Task DeleteHourlyUsageByDateAsync(DateTime date)
    {
        await InitAsync();
        await _database.ExecuteAsync(
            "DELETE FROM HourlyUsageRecords WHERE Date = ?",
            date.Date);
    }

    /// <summary>
    /// 删除指定 ID 的每小时使用记录
    /// </summary>
    public async Task DeleteHourlyUsageByIdAsync(int id)
    {
        await InitAsync();
        await _database.DeleteAsync<HourlyUsageRecord>(id);
    }

    /// <summary>
    /// 获取每日聚合记录
    /// </summary>
    public async Task<List<DailyAggregateRecord>> GetDailyAggregatesAsync(DateTime startDate, DateTime endDate)
    {
        await InitAsync();
        return await _database.Table<DailyAggregateRecord>()
            .Where(r => r.Date >= startDate.Date && r.Date <= endDate.Date)
            .OrderByDescending(r => r.Date)
            .ToListAsync();
    }

    // ===== Phase 6: 聚合服务辅助方法 =====

    /// <summary>
    /// 获取指定日期的休息任务记录
    /// </summary>
    public async Task<List<BreakTaskRecord>> GetBreakTasksForDateAsync(DateTime date)
    {
        await InitAsync();
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await _database.Table<BreakTaskRecord>()
            .Where(r => r.CreatedAt >= startOfDay && r.CreatedAt < endOfDay)
            .ToListAsync();
    }

    /// <summary>
    /// 获取指定日期范围的疲劳快照（用于聚合计算）
    /// </summary>
    public async Task<List<FatigueSnapshot>> GetFatigueSnapshotsForDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        await InitAsync();
        return await _database.Table<FatigueSnapshot>()
            .Where(s => s.Date >= startDate.Date && s.Date <= endDate.Date)
            .OrderBy(s => s.RecordedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 删除指定日期的休息任务记录（聚合后清理）
    /// </summary>
    public async Task DeleteBreakTasksByDateAsync(DateTime date)
    {
        await InitAsync();
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        await _database.ExecuteAsync(
            "DELETE FROM BreakTaskRecords WHERE CreatedAt >= ? AND CreatedAt < ?",
            startOfDay, endOfDay);
    }
}
