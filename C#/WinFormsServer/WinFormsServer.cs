using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SignalRChat
{
    /// <summary>
    /// WinForms host for a SignalR server. The host can stop and start the SignalR
    /// server, report errors when trying to start the server on a URI where a
    /// server is already being hosted, and monitor when clients connect and disconnect. 
    /// The hub used in this server is a simple echo service, and has the same 
    /// functionality as the other hubs in the SignalR Getting Started tutorials.
    /// </summary>
    public partial class WinFormsServer : Form
    {
        private IDisposable SignalR { get; set; }
        const string ServerURI = "http://localhost:8080";

        public static string[] a = { "Столица Франции", "Первый элемент таблицы Менделеева", "5-я буква с конца русского алфавита", "Семь раз отмерь, один раз ...", "Основатель Телеграм" };
        public static Dictionary<string, string> dic = new Dictionary<string, string>
        {
            ["Столица Франции"] = "Париж",
            ["Первый элемент таблицы Менделеева"] = "Водород",
            ["5-я буква с конца русского алфавита"] = "Ы",
            ["Семь раз отмерь, один раз ..."] = "отрежь",
            ["Основатель Телеграм"] = "Павел Дуров",
        };

        public static Dictionary<string, int> dicc = new Dictionary<string, int>
        {
            ["Викторина"] = 0,
        };
        public static String vopros = "Столица Франции";

        internal WinFormsServer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Calls the StartServer method with Task.Run to not
        /// block the UI thread. 
        /// </summary>
        private void ButtonStart_Click(object sender, EventArgs e)
        {
            WriteToConsole("Starting server...");
            ButtonStart.Enabled = false;
            Task.Run(() => StartServer());
        }

        /// <summary>
        /// Stops the server and closes the form. Restart functionality omitted
        /// for clarity.
        /// </summary>
        private void ButtonStop_Click(object sender, EventArgs e)
        {
            //SignalR will be disposed in the FormClosing event
            Close();
        }

        /// <summary>
        /// Starts the server and checks for error thrown when another server is already 
        /// running. This method is called asynchronously from Button_Start.
        /// </summary>
        private void StartServer()
        {
            try
            {
                SignalR = WebApp.Start(ServerURI);
            }
            catch (TargetInvocationException)
            {
                WriteToConsole("Server failed to start. A server is already running on " + ServerURI);
                //Re-enable button to let user try to start server again
                this.Invoke((Action)(() => ButtonStart.Enabled = true));
                return;
            }
            this.Invoke((Action)(() => ButtonStop.Enabled = true));
            WriteToConsole("Server started at " + ServerURI);
            Vopros();
        }
        /// <summary>
        /// This method adds a line to the RichTextBoxConsole control, using Invoke if used
        /// from a SignalR hub thread rather than the UI thread.
        /// </summary>
        /// <param name="message"></param>
        internal void WriteToConsole(String message)
        {
            if (RichTextBoxConsole.InvokeRequired)
            {
                this.Invoke((Action)(() =>
                    WriteToConsole(message)
                ));
                return;
            }
            RichTextBoxConsole.AppendText(message + Environment.NewLine);
        }
        public void Vopros()
        {
            var aTimer = new System.Timers.Timer(1000);

            aTimer.Elapsed += aTimer_Elapsed;
            aTimer.Interval = 15000;
            aTimer.Enabled = true;
        }

        private void aTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<MyHub>();

            vopros = a[new Random().Next(0, a.Length)];
            context.Clients.All.addMessage("Викторина", vopros);

        }


        private void WinFormsServer_FormClosing(object sender, FormClosingEventArgs e)
        {
            
            if (SignalR != null)
            {
                SignalR.Dispose();
            }
        }
    }
    /// <summary>
    /// Used by OWIN's startup process. 
    /// </summary>
    class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR();
        }
    }
    /// <summary>
    /// Echoes messages sent using the Send message by calling the
    /// addMessage method on the client. Also reports to the console
    /// when clients connect and disconnect.
    /// </summary>
    public class MyHub : Hub
    {
        public void Send(string name, string message)
        {
            Clients.All.addMessage(name, message);
        }
        public void SendOtvet(string name, string message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<MyHub>();
            string ss = WinFormsServer.vopros; 
            string k = WinFormsServer.dic.Where(x => x.Value == message).FirstOrDefault().Key;
            if (!WinFormsServer.dicc.ContainsKey(name))
            {
                WinFormsServer.dicc.Add(name, 0);
                Console.WriteLine("Добавлено");
            }
            if (k != null && WinFormsServer.dic[ss] == message)
            {
                WinFormsServer.dicc[name] = (WinFormsServer.dicc[name]) + 1;
                context.Clients.All.addMessage("Викторина", "Ответ верный! Игрок: " + name + " получает " + WinFormsServer.dicc[name] + " очка(-ов).");
            }
            else
            {
                context.Clients.All.addMessage("Викторина", "Неверно! Попробуй еще раз.");
            }
        }
        public override Task OnConnected()
        {
            Program.MainForm.WriteToConsole("Client connected: " + Context.ConnectionId);
            var context = GlobalHost.ConnectionManager.GetHubContext<MyHub>();
            context.Clients.Client(Context.ConnectionId).addMessage("Викторина", WinFormsServer.vopros);
            return base.OnConnected();
        }
        public override Task OnDisconnected()
        {
            Program.MainForm.WriteToConsole("Client disconnected: " + Context.ConnectionId);
            return base.OnDisconnected();
        }
    }
}
