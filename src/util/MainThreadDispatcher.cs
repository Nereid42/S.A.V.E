using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Nereid
{
   namespace SAVE
   {
      [KSPAddon(KSPAddon.Startup.Instantly, true)]
      public class MainThreadDispatcher : MonoBehaviour
      {
         static public MainThreadDispatcher Instance;
         static private volatile bool IsQueued = false;
         static private List<Action> Backlog = new List<Action>();
         static private List<Action> Actions = new List<Action>();

         public void Awake()
         {
            Debug.Log("S.A.V.E: awaking instance of MainThreadDispatcher");
            Instance = this;
            DontDestroyOnLoad(this);
         }

         private void Start()
         {
            Debug.Log("S.A.V.E: starting instance of MainThreadDispatcher");
         }

         public static void RunOnMainThread(Action action)
         {
            lock (Backlog)
            {
               Backlog.Add(action);
               IsQueued = true;
            }
         }

         private void Update()
         {
            if (IsQueued)
            {
               lock (Backlog)
               {
                  var tmp = Actions;
                  Actions = Backlog;
                  Backlog = tmp;
                  IsQueued = false;
               }

               foreach (var action in Actions)
                  action();

               Actions.Clear();
            }
         }

      }
   }
}
