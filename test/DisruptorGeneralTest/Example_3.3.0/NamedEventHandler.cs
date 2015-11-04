using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;

namespace DisruptorGeneralTest.Example_3._3._0
{
   public class NamedEventHandler<T> : IEventHandler<T>, ILifecycleAware
{
    private String oldName;
    private readonly String name;

    public NamedEventHandler( String name)
    {
        this.name = name;
    }

  
    public void OnEvent( T @event,  long sequence,  bool endOfBatch)
    {
    }

   
    public void OnStart()
    {
         Thread currentThread = Thread.CurrentThread;
         oldName = Thread.CurrentThread.Name;
         Thread.CurrentThread.Name=name;
    }

  
    public void OnShutdown()
    {
        Thread.CurrentThread.Name=oldName;
    }
}

}
