using System;
using System.IO;
using System.Diagnostics;

namespace WhatTheVersion {

    class Program {

        const string DayPlaceholder      = "{DD}";
        const string MonthPlaceholder    = "{MM}";
        const string YearPlaceholder     = "{YYYY}";
        const string SVNPlaceholder      = "{WCREV}";
        const string SubWCrevPlaceholder = "$WCREV$";



        /// <summary>
        /// Application Entry Point
        /// 
        /// Expecting the following command-line arguments:
        /// file-in
        /// file-out
        /// path-to-SubWCrev.exe (optional)
        /// svn-working-copy-path (required if path-to-SubWCrev.exe is given)
        /// </summary>
        static int Main(string[] args) {

            // is the usage correct?
            if (!(args.Length == 2 || args.Length == 4))
                return PrintUsage(ExitCode.WTV_Wrong_No_Of_Arguments);



            string inputFileContents = string.Empty;
            string outputFileContents = string.Empty;


            // read the input file
            try {
                inputFileContents = File.ReadAllText(args[0]);
            } catch (Exception) {
                return PrintUsage(ExitCode.WTV_Problem_Reading_Input_File);
            }

            
            // do the replacements
            try {
                outputFileContents = DoReplacements(inputFileContents, args);
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                return PrintUsage(ExitCode.WTV_Problem_Running_SubWCrev);
            }
            

            // write out
            try	{
		        File.WriteAllText(args[1], outputFileContents);
	        }
	        catch (Exception) {
                return PrintUsage(ExitCode.WTV_Problem_Writing_To_Output_File);
	        }


            // done
            return (int)ExitCode.Success;
        }



        /// <summary>
        /// Replaces the Date and SVN placeholders with the actual values
        /// </summary>
        private static string DoReplacements(string inputFileContents, string[] args) {
            DateTime now = DateTime.Now;

            return inputFileContents
                .Replace(DayPlaceholder, now.Day.ToString())
                .Replace(MonthPlaceholder, now.Month.ToString())
                .Replace(YearPlaceholder, now.Year.ToString())
                .Replace(SVNPlaceholder, GetSVNRevisionNumber(args).ToString());
        }



        /// <summary>
        /// Gets the highest SVN Revision Number for the Project Path passed in
        /// </summary>
        private static short GetSVNRevisionNumber(string[] args) {
            short svnRevisionNumber = 0;

            if (args.Length == 4) {
                
                // is the given path to SubWCrev.exe OK?
                if (!File.Exists(args[2]))
                    throw new ArgumentException("SubWCrev.exe not found at: " + args[2]);

                // is the Project path OK?
                if (!Directory.Exists(args[3]))
                    throw new ArgumentException("Project path looks invalid: " + args[3]);


                // create a temporary file within the project root to call SubWCrev.exe on
                string tempFilename = Path.GetTempFileName();
                try {
                    File.WriteAllText(tempFilename, SubWCrevPlaceholder);

                    // call SubWCrev.exe on our temporary file
                    ProcessStartInfo subWCrevProcessInfo = new ProcessStartInfo(
                        args[2],
                        args[3] + " " + tempFilename + " " + tempFilename)
                        { CreateNoWindow = true };

                    Process subWCrevProcessCall = Process.Start(subWCrevProcessInfo);
                    subWCrevProcessCall.WaitForExit();

                    // read the result back in
                    string tempFilenameContents = File.ReadAllText(tempFilename);

                    // massage it to fit into a short / Int16
                    if (long.Parse(tempFilenameContents) <= short.MaxValue)
                        svnRevisionNumber = short.Parse(tempFilenameContents);
                    else
                        svnRevisionNumber = short.Parse(
                             tempFilenameContents.Substring(
                                 tempFilenameContents.Length - 4,
                                 4)
                             );     // revision is too big for Int16, so just take the last four digits

                } catch (Exception ex) {
                    throw new ArgumentNullException("Problem running SubWCrev.exe", ex);
                } finally {
                    // tidy up
                    try {
                        File.Delete(tempFilename);
                    } catch (Exception) { }

                }
            }

            return svnRevisionNumber;
        }




        /// <summary>
        /// Prints usage instructions to the Console, and returns the int value for the ExitCode passed in
        /// </summary>
        private static int PrintUsage(ExitCode exitCode) {
            ConsoleColor originalColor = Console.ForegroundColor;

            Console.WriteLine("");
            Console.WriteLine("  WTV (When The Version) Date-based version numbering for .Net projects");
            Console.WriteLine("  Andrew Freemantle - www.fatlemon.co.uk");
            Console.WriteLine("");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("    Error: " + exitCode.ToString().Replace("_", " "));

            Console.ForegroundColor = originalColor;
            Console.WriteLine("");
            Console.WriteLine("  Usage: WTV  \"file-in\"  \"file-out\"  [\"path to SubWCrev.exe\"  \"SVN working-copy-path\"]");
            Console.WriteLine("   \"file-in\"  can contain the following placeholders:");
            Console.WriteLine("     {DD}     - Day");
            Console.WriteLine("     {MM}     - Month");
            Console.WriteLine("     {YYYY}   - Year");
            Console.WriteLine("     {WCREV}  - SubVersion revision (must specify the path to SubWCrev.exe and working copy path)");
            Console.WriteLine("");
            Console.WriteLine("  Example Pre-Build command: (remove the line breaks)");
            Console.WriteLine("    \"C:\\Path\\To\\WTV.exe\"");
            Console.WriteLine("      \"$(ProjectDir)Properties\\AssemblyInfo.Template.cs\"");
            Console.WriteLine("      \"$(ProjectDir)Properties\\AssemblyInfo.cs\"");
            Console.WriteLine("      \"C:\\Program Files\\TortoiseSVN\\bin\\SubWCRev.exe\"");
            Console.WriteLine("      \"$(SolutionDir).\"");

            return (int)exitCode;
        }






        /// <summary>
        /// Enumeration for exit / return codes
        /// </summary>
        enum ExitCode : int {
            Success = 0,
            WTV_Wrong_No_Of_Arguments = 1,
            WTV_Problem_Reading_Input_File = 2,
            WTV_Problem_Writing_To_Output_File = 3,
            WTV_Problem_Running_SubWCrev = 4,
            WTV_Problem_Getting_SVN_Revision_Number = 5

        }


    }
}
