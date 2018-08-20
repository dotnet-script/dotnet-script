//using System.IO;

//namespace Dotnet.Script.Core
//{
//    public class ScriptLogger
//    {
//        private readonly TextWriter _writer;
//        private readonly bool _isDebug;

//        public ScriptLogger(TextWriter writer, bool isDebug)
//        {
//            _writer = writer ?? TextWriter.Null;
//            _isDebug = isDebug;
//        }

//        public void Log(string message)
//        {
//            _writer.WriteLine(message);
//        }

//        public void Verbose(string message)
//        {
//            if (_isDebug)
//            {
//                Log(message);
//            }
//        }
//    }
//}