using CommandLine;

namespace CalendarExport
{
    class Program
    {
        public static int Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<CalendarExportArguments>(args);
            if (result.Tag == ParserResultType.NotParsed)
            {
                // help is automatically printed
                return 1;
            }

            CalendarExportArguments parsedArguments = null;
            result.WithParsed(x => parsedArguments = x);

            if (!parsedArguments.Validate())
            {
                return 1;
            }

            bool success = new Exporter(parsedArguments).DoMagic();
            
            return success ? 0 : 1;
        }
    }
}
