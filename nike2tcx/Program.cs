using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using CommandLine;
using CommandLine.Text;
using net.sf.saxon;

namespace nike2tcx
{
    public class Options
    {
        [Option('d', "dir", Required = true, HelpText = "Path to directory with Nike+ workouts")]
        public string WorkoutsDir { get; set; }

        [Option('o', "out", DefaultValue = @"nikeplus_workouts.tcx", HelpText = "Path to the output TCX file")]
        public string OutPath { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this);
        }
    }

    class Program
    {
        public const string NIKETOTCX_XSL = "nike+totcx.xsl";

        public const string TCX_TEMPLATE = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<TrainingCenterDatabase xmlns:xs=""http://www.w3.org/2001/XMLSchema""
                        xmlns:ns5=""http://www.garmin.com/xmlschemas/ActivityGoals/v1""
                        xmlns:ns3=""http://www.garmin.com/xmlschemas/ActivityExtension/v2""
                        xmlns:ns2=""http://www.garmin.com/xmlschemas/UserProfile/v2""
                        xmlns=""http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2""
                        xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                        xmlns:ns4=""http://www.garmin.com/xmlschemas/ProfileExtension/v1"">
   <Activities>
   </Activities>
</TrainingCenterDatabase>";

        static void Main(string[] args)
        {
            var options = new Options();

            if (!Parser.Default.ParseArguments(args, options))
            {
                Environment.Exit(Parser.DefaultExitCodeFail);
            }

            if (!Directory.Exists(options.WorkoutsDir))
            {
                Console.WriteLine("ERROR: Invalid workouts dir path");
                Environment.Exit(-1);
            }

            ConvertNikeDirToTcx(options);
        }

        static void ConvertNikeDirToTcx(Options options)
        {
            File.WriteAllText(options.OutPath, TCX_TEMPLATE);

            var tcx = XDocument.Load(options.OutPath);
            var ns = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2";
            var activities = tcx.Root.Element(XName.Get("Activities", ns));

            var tempTcx = Path.GetTempFileName();

            foreach (var workoutFile in Directory.GetFiles(options.WorkoutsDir))
            {
                Console.WriteLine("Converting '{0}'...", Path.GetFileName(workoutFile));
                TransformNikeToTcx(workoutFile, tempTcx);

                var temp = XDocument.Load(tempTcx);
                var activity = temp.Root
                    .Element(XName.Get("Activities", ns))
                    .Element(XName.Get("Activity", ns));
                activities.Add(activity);

                File.Delete(tempTcx);
            }

            tcx.Save(options.OutPath);
        }

        static void TransformNikeToTcx(string sourcePath, string outPath)
        {
            var saxonArgs = new[]
            {
                string.Format("-s:{0}", Path.GetFullPath(sourcePath)),
                string.Format("-xsl:{0}", Path.GetFullPath(NIKETOTCX_XSL)),
                string.Format("-o:{0}", Path.GetFullPath(outPath))
            };

            new Transform().doTransform(saxonArgs, "Transform");
        }
    }
}
