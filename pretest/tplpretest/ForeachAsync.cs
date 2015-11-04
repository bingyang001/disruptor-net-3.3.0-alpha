using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace tplpretest
{
    static class ForeachAsync
    {
        public static Task ForEachAsync<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, Task<TResult>> taskSelector, Action<TSource, TResult> resultProcessor)
        {
            var oneAtATime = new SemaphoreSlim(initialCount: 1, maxCount: 1);
            return Task.WhenAll(
                from item in source
                select ProcessAsync(item, taskSelector, resultProcessor, oneAtATime));
        }

        private static async Task ProcessAsync<TSource, TResult>(
            TSource item,
            Func<TSource, Task<TResult>> taskSelector, Action<TSource, TResult> resultProcessor,
            SemaphoreSlim oneAtATime)
        {
            TResult result = await taskSelector(item);
            await oneAtATime.WaitAsync();
            try { resultProcessor(item, result); }
            finally { oneAtATime.Release(); }
        }
        public static async Task ForEachAsync2<TSource, TResult>(this IEnumerable<TSource> source,
        Func<TSource, Task<TResult>> taskSelector, Action< TResult> resultProcessor)
        {
            var taskSelectorBlock = new TransformBlock<TSource, TResult>(
                taskSelector,
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded });
            var resultProcessorBlock = new ActionBlock<TResult>(resultProcessor);
            taskSelectorBlock.LinkTo(resultProcessorBlock, new DataflowLinkOptions { PropagateCompletion = true });
            foreach (var item in source)
                await taskSelectorBlock.SendAsync(item);
            taskSelectorBlock.Complete();
            await resultProcessorBlock.Completion;
        }
    }
}
