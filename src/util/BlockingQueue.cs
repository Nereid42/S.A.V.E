using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Nereid
{
   namespace SAVE
   {
      class BlockingQueue<T>
      {
         private Queue<T> queue = new Queue<T>();
         readonly object locker = new object();

         public T Dequeue()
         {
            lock (locker)
            {
               while (queue.Count == 0)
               {
                  Monitor.Wait(locker);
               }
            }
            return queue.Dequeue();
         }

         public void Enqueue(T item)
         {
            lock (locker)
            {
               queue.Enqueue(item);
               Monitor.Pulse(locker);
            }
         }

         public int Size()
         {
            return queue.Count;
         }
      }
   }
}
