using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WeClean
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int LOG_LINE_LIMIT = 99;
        private const int WALK_TIME_WAIT = 10;
        private IEnumerator<FileInfo> fileEnumerator;
        private IEnumerator<DirectoryInfo> dirEnumerator;

        private readonly Random random = new Random();
        private readonly Queue<string> queue = new Queue<string>();
        private int length;

        private readonly string[] phrases =
        {
            "Moving {0} to {1}",
            "Scanning for programs using {0}",
            "Cleaning {0}",
            "Adding checksums in {1}",
        };

        private string phrase;
        private int phraseIndex;

        private readonly ConcurrentQueue<string> addedValues = new ConcurrentQueue<string>();
        private readonly Task thread;

        public MainWindow()
        {
            InitializeComponent();


            new DispatcherTimer(TimeSpan.FromMilliseconds(WALK_TIME_WAIT), DispatcherPriority.Normal,
                DispatcherTimer_Callback,
                Dispatcher ?? throw new InvalidOperationException()).Start();


            fileEnumerator = DirectoryEnumerator.WalkFiles(GetRandomDirectory(), random).GetEnumerator();
            dirEnumerator = DirectoryEnumerator.WalkDirectories(GetRandomDirectory(), random).GetEnumerator();
            phrase = phrases[phraseIndex];

            thread = new Task(IterateFileAndDirThread);
            thread.ContinueWith(TaskExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
            thread.Start();
        }

        private void TaskExceptionHandler(Task obj)
        {
            throw obj.Exception;
        }

        private void DispatcherTimer_Callback(object sender, EventArgs e)
        {
            int count = 0;

            while (addedValues.TryDequeue(out string value) && count < LOG_LINE_LIMIT)
            {
                length += value.Length;
                queue.Enqueue(value);
                count++;
            }

            if (thread.IsCompleted || thread.IsFaulted || thread.IsCanceled)
            {
                throw new ApplicationException();
            }

            if (count == 0)
            {
                return;
            }

            while (queue.Count > LOG_LINE_LIMIT)
            {
                length -= queue.Dequeue().Length;
            }

            var sb = new StringBuilder(length + queue.Count * Environment.NewLine.Length);
            string last = null;
            foreach (string line in queue)
            {
                sb.AppendLine(line);
                last = line;
            }

            LogBlock.Text = sb.ToString();
            LogScroll.ScrollToBottom();
            StatusLabel.Content = last;
        }

        private void IterateFileAndDirThread()
        {
            Random random = new Random();
            while (true)
            {
                try
                {
                    if (addedValues.Count > LOG_LINE_LIMIT)
                    {
                        continue;
                    }

                    if (random.Next(100) > 30)
                    {
                        phraseIndex = (phraseIndex + 1) % phrases.Length;
                        phrase = phrases[phraseIndex];
                    }

                    string str = string.Format(phrase,
                        IterateFileEnumerator(random).FullName,
                        IterateDirEnumerator(random).FullName);
                    addedValues.Enqueue(str);
                    Thread.Sleep(10);
                }
                catch
                {
                    // Do nothing
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
        }

        FileInfo IterateFileEnumerator(Random provider)
        {
            if (fileEnumerator.MoveNext())
            {
                return fileEnumerator.Current;
            }
            else
            {
                fileEnumerator = DirectoryEnumerator.WalkFiles(GetRandomDirectory(), provider).GetEnumerator();
                fileEnumerator.MoveNext();
                return fileEnumerator.Current;
            }
        }

        DirectoryInfo IterateDirEnumerator(Random provider)
        {
            if (dirEnumerator.MoveNext())
            {
                return dirEnumerator.Current;
            }
            else
            {
                dirEnumerator = DirectoryEnumerator.WalkDirectories(GetRandomDirectory(), provider).GetEnumerator();
                dirEnumerator.MoveNext();
                return dirEnumerator.Current;
            }
        }

        DirectoryInfo GetRandomDirectory()
        {
            var drives = DriveInfo.GetDrives().Where(o => o.IsReady).ToArray();

            if (drives.Length == 0)
            {
                throw new ApplicationException("No drives found.");
            }

            return drives[random.Next(0, drives.Length)].RootDirectory;
        }
    }
}