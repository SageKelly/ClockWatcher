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
        public MethodInfo PrintMethod;
        public string MethodName
        {
            get
            {
                return PrintMethod.Name;
            }
            private set { }
        }
        public object[] MethodParams;

        public List<PrintTask> Subtasks;

        public PrintTask(MethodInfo PrintMethod, object[] MethodParams)
        {
            this.PrintMethod = PrintMethod;
            MethodName = PrintMethod.Name;
            this.MethodParams = MethodParams;
            Subtasks = new List<PrintTask>();
        }

        public void Invoke()
        {
            PrintMethod.Invoke(null, MethodParams);
            foreach (PrintTask pt in Subtasks)
            {
                pt.Invoke();
            }
        }

    }
}
