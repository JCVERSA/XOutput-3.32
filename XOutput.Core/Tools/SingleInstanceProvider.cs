using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XOutput.Logging;

namespace XOutput.Tools
{
    public class SingleInstanceProvider
    {
        private static readonly ILogger logger = LoggerFactory.GetLogger(typeof(SingleInstanceProvider));

        private const string MutexName = "XOutputRunningAlreadyMutex";
        private const string PipeName = "XOutputRunningAlreadyNamedPipe";
        private const string ShowCommand = "Show";
        private const string OkResponse = "OK";
        private const string ErrorResponse = "ERROR";

        public event Action ShowEvent;

        private Thread notifyThread;
        private readonly Mutex mutex = new Mutex(false, MutexName);

        public bool TryGetLock()
        {
            try
            {
                return mutex.WaitOne(0, false);
            }
            catch (AbandonedMutexException)
            {
                // A previous instance crashed without releasing the mutex (e.g. the
                // process was killed). The mutex is now owned by this thread — treat
                // it as acquired instead of failing startup.
                logger.Warning("Abandoned mutex detected; a previous XOutput instance did not shut down cleanly.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }

        public void ReleaseLock()
        {
            mutex.ReleaseMutex();
        }

        public void Close()
        {
            mutex.Close();
        }

        public void StartNamedPipe()
        {
            notifyThread = new Thread(() => ReadPipe());
            notifyThread.IsBackground = true;
            notifyThread.Name = "XOutputRunningAlreadyNamedPipe reader";
            notifyThread.Start();
        }

        public void StopNamedPipe()
        {
            notifyThread?.Interrupt();
        }

        public bool Notify()
        {
            try
            {
                using (var client = new NamedPipeClientStream(PipeName))
                {
                    // Never block forever: a stale process may hold the mutex while its
                    // pipe server is dead, and an unbounded Connect() would leave the
                    // new instance hanging with no window (the classic "app won't open").
                    client.Connect(2000);
                    client.ReadTimeout = 2000;
                    client.WriteTimeout = 2000;
                    StreamString ss = new StreamString(client);
                    ss.WriteString(ShowCommand);
                    return ss.ReadString() == OkResponse;
                }
            }
            catch (Exception ex)
            {
                logger.Error("Failed to notify the running instance: " + ex.Message);
                return false;
            }
        }

        private void ReadPipe()
        {
            bool running = true;
            while (running)
            {
                using (var notifyWait = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1))
                {
                    notifyWait.WaitForConnection();
                    try
                    {
                        StreamString ss = new StreamString(notifyWait);
                        string command = ss.ReadString();
                        ss.WriteString(ProcessCommand(command));
                    }
                    catch(ThreadInterruptedException)
                    {
                        running = false;
                    }
                    catch (IOException e)
                    {
                        logger.Error(e);
                    }
                }
            }
        }

        private string ProcessCommand(string request)
        {
            if(request == ShowCommand)
            {
                ShowEvent?.Invoke();
                return OkResponse;
            }
            return ErrorResponse;
        }
    }

    class StreamString
    {
        private Stream ioStream;
        private UnicodeEncoding streamEncoding;

        public StreamString(Stream ioStream)
        {
            this.ioStream = ioStream;
            streamEncoding = new UnicodeEncoding();
        }

        public string ReadString()
        {
            int len;
            len = ioStream.ReadByte() * 256;
            len += ioStream.ReadByte();
            byte[] inBuffer = new byte[len];
            ioStream.ReadExactly(inBuffer, 0, len);

            return streamEncoding.GetString(inBuffer);
        }

        public int WriteString(string outString)
        {
            byte[] outBuffer = streamEncoding.GetBytes(outString);
            int len = outBuffer.Length;
            if (len > UInt16.MaxValue)
            {
                len = (int)UInt16.MaxValue;
            }
            ioStream.WriteByte((byte)(len / 256));
            ioStream.WriteByte((byte)(len & 255));
            ioStream.Write(outBuffer, 0, len);
            ioStream.Flush();

            return outBuffer.Length + 2;
        }
    }
}
