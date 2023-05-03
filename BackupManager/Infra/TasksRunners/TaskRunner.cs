using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BackupManager.Infra.TasksRunners;

// TODO DOR into common.
public class TasksRunner
{
    private readonly Task?[] mAllRunningTasks;
    private readonly TimeSpan mIntervalForCheckingAvailableSlot = TimeSpan.FromMilliseconds(500);
    private readonly ILogger<TasksRunner> mLogger;
    
    public TasksRunner(ushort allowedParallelTasks, ILogger<TasksRunner> logger)
    {
        ushort fixedAllowedParallelTasks = allowedParallelTasks == 0u ? (ushort)1 : allowedParallelTasks;
        mAllRunningTasks = new Task[fixedAllowedParallelTasks];
        mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // TODO DOR add tests.
    public void RunTask(Task task, CancellationToken cancellationToken)
    {
        ushort? indexToPlaceTask = null;

        while (indexToPlaceTask is null)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                mLogger.LogInformation($"Cancel requested");
                break;
            }
            
            indexToPlaceTask = tryFindIndexToLocateTask();
            Task.Delay(mIntervalForCheckingAvailableSlot, cancellationToken);
        }

        if (indexToPlaceTask is null)
        {
            return;
        }
        
        insertTaskByIndex(indexToPlaceTask.Value, task);
    }

    // TODO DOR add tests.
    public bool WaitAll(CancellationToken cancellationToken)
    {
        for (ushort i = 0; i < mAllRunningTasks.Length; ++i)
        {
            Task? task = mAllRunningTasks[i];

            while (task is not null && !task.IsCompleted)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    mLogger.LogInformation($"Cancel requested");
                    return false;
                }
                
                Task.Delay(mIntervalForCheckingAvailableSlot, cancellationToken);
            }
        }

        return true;
    }

    private ushort? tryFindIndexToLocateTask()
    {
        for (ushort i = 0; i < mAllRunningTasks.Length; ++i)
        {
            Task? task = mAllRunningTasks[i];
            if (task is null || task.IsCompleted)
            {
                return i;
            }
        }

        return null;
    }

    private void insertTaskByIndex(ushort indexToPlaceTask, Task task)
    {
        Task? oldTask = mAllRunningTasks[indexToPlaceTask];
        oldTask?.Dispose();

        mAllRunningTasks[indexToPlaceTask] = task;
    }
}