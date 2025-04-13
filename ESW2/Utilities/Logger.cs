namespace ESW2.Utilities
{
    public class Logger
    {
        private static Logger instance;
        private static readonly object lockObject = new object();

        private Logger() { }

        public static Logger GetInstance()
        {
            if (instance == null)
            {
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        instance = new Logger();
                    }
                }
            }
            return instance;
        }

        public void Log(string message)
        {
            Console.WriteLine($"[LOG] {DateTime.Now}: {message}");
        }
    }
}