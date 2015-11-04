using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using NUnit.Framework;

namespace tplpretest
{
    [TestFixture]
    public class TplPerTest
    {
        [Test]
        public async Task ActionBlockTest()
        {
            var sw = new Stopwatch();
            const long ITERS = 100000000;
            Console.WriteLine("test count {0:N0}",ITERS);
            var are = new AutoResetEvent(false);

            var ab = new ActionBlock<int>(i => { if (i == ITERS) are.Set(); });
            for(var c=0;c<20;c++)
            {
                sw.Restart();
                for (int i = 1; i <= ITERS; i++)
                   await ab.SendAsync(i);
                are.WaitOne();
                sw.Stop();
                Console.WriteLine("test {0},Messages / sec: {1:N0} ops,run {2} ms",
                    c,(ITERS * 1000 / sw.ElapsedMilliseconds), sw.ElapsedMilliseconds);
            }
        }
        [Test]
        public async Task ForeachAsyncTask() 
        {
            var client = new HttpClient();
          var results = new Dictionary<string, string>();
          var urlList=new List<string>();
          urlList.Add("http://blogs.msdn.com/b/pfxteam/archive/2012/03/04/10277325.aspx");
          urlList.Add("http://stackoverflow.com/questions/19189275/asynchronously-and-parallelly-downloading-files");
          urlList.Add("https://msdn.microsoft.com/zh-cn/library/hh194782(v=vs.110).aspx");
          urlList.Add("http://blog.sina.com.cn/");
          await urlList.ForEachAsync(url => client.GetStringAsync(url), (url, contents) => results.Add(url, contents));

          foreach (var i in results) 
          {
             Console.WriteLine(i.Value.Length );
           }
          results = new Dictionary<string, string>();
          await urlList.ForEachAsync2(url => client.GetStringAsync(url), r => results.Add(r, r));

          foreach (var i in results)
          {
              Console.WriteLine(i.Value.Length);
          }
        }
    }
}
