using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TimeKeeper
{
    public class PrintTask
    {
        public delegate void PrintCompleteEventHandler();
        public MethodInfo PrintMethod;
        public object[] MethodParams;
        public event PrintCompleteEventHandler PrintCompleteEvent;

        public PrintTask(MethodInfo PrintMethod, object[] MethodParams)
        {
            this.PrintMethod = PrintMethod;
            this.MethodParams = MethodParams;
        }


        public PrintTask(MethodInfo PrintMethod, object[] MethodParams, PrintCompleteEventHandler EventHandlerMethod) : this(PrintMethod, MethodParams)
        {
            PrintCompleteEvent += EventHandlerMethod;
        }

        public void Invoke()
        {
            PrintMethod.Invoke(null, MethodParams);
            PrintCompleteEvent?.Invoke();
        }

    }
}
