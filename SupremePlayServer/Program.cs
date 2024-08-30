using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SupremePlayServer
{
    static class Program
    {
        /// <summary>
        /// 해당 응용 프로그램의 주 진입점입니다.
        /// </summary>
        static public mainForm mainForm;
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            mainForm = new mainForm();
            Application.Run(mainForm);
        }

        // Do OnProcessExit
        // 스레드 안남기고 다 꺼지게 함
        static void OnProcessExit(object sender, EventArgs e)
        {
            Application.ExitThread();
            Environment.Exit(0);
            MessageBox.Show(e.ToString());
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}
