using System;
using System.IO;
using System.Diagnostics;

namespace WhatTheVersion {

    class Program {

        const string DayPlaceholder      = "{DD}";
        const string MonthPlaceholder    = "{MM}";
        const string YearPlaceholder     = "{YYYY}";
        const string SVNPlaceholder      = "{SVN}";
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
            } catch (Exception) {
                return PrintUsage(ExitCode.WTV_Problem_Doing_Replacements);
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
            DateTime now = DateTime.UtcNow;

            return inputFileContents
                .Replace(DayPlaceholder, now.Day.ToString())
                .Replace(MonthPlaceholder, now.Month.ToString())
                .Replace(YearPlaceholder, now.Year.ToString())
                .Replace(SVNPlaceholder, GetSVNRevisionNumber(args).ToString());
        }



        /// <summary>
        /// Gets the highest SVN Revision Number for the Project Path passed in
        /// </summary>
        /// <remarks>
        /// Always returns a value, but writes errors to the Console (returning 0)
        /// </remarks>
        private static ushort GetSVNRevisionNumber(string[] args) {
            ushort svnRevisionNumber = 0;

            if (args.Length == 4) {

                // is the given path to SubWCrev.exe OK?
                if (!File.Exists(args[2])) {
                    // hmm, file doesn't exist, is this an environment variable containing the path instead?
                    try {	        
		                args[2] = Environment.GetEnvironmentVariable(args[2]);
	                }
	                catch (Exception) {	/* exception doesn't matter */ }
                }

                // try again in case we've found an environment variable..
                if (!File.Exists(args[2])) {
                    WriteErrorMessageToConsole("SubWCrev.exe not found at: " + args[2]);
                    return svnRevisionNumber;
                }

                // do the call to SubWCRev.exe..
                try {	        
		            svnRevisionNumber = CallSubWCrev(args);
	            } catch (Exception ex) {
		            WriteErrorMessageToConsole(ex.Message);
	            }


            }

            return svnRevisionNumber;

        }



        /// <summary>
        /// Calls SubWCRev.exe to get the Subversion Working Copy Revision number
        /// </summary>
        /// <returns></returns>
        private static ushort CallSubWCrev(string[] args) {
            ushort svnRevisionNumber = 0;

            // create a temporary file call SubWCrev.exe on
            string tempFilename = Path.GetTempFileName();

            try {

                File.WriteAllText(tempFilename, SubWCrevPlaceholder);


                // make sure working copy path is enclosed in quotes
                //  in case the path contains spaces..
                string workingCopyPath = args[3].StartsWith("\"")
                    ? args[3] : "\"" + args[3] + "\"";

                // call SubWCrev.exe on our temporary file
                ProcessStartInfo subWCrevProcessInfo = new ProcessStartInfo(
                    args[2],
                    workingCopyPath + " \"" + tempFilename + "\" \"" + tempFilename + "\"") { CreateNoWindow = true };

                Process subWCrevProcessCall = Process.Start(subWCrevProcessInfo);
                subWCrevProcessCall.WaitForExit();



                // did it work?
                //  SubWCRev.exe error codes:
                //  source: http://code.google.com/p/tortoisesvn/source/browse/trunk/src/SubWCRev/SubWCRev.cpp
                switch (subWCrevProcessCall.ExitCode) {
                    case 0:
                        // Call completed OK

                        // read the result back in
                        string tempFilenameContents = File.ReadAllText(tempFilename);

                        // massage it to fit into a ushort / UInt16
                        if (long.Parse(tempFilenameContents) <= ushort.MaxValue)
                            svnRevisionNumber = ushort.Parse(tempFilenameContents);
                        else
                            svnRevisionNumber = ushort.Parse(
                                    tempFilenameContents.Substring(
                                        tempFilenameContents.Length - 4,
                                        4)
                                    );     // revision is too big for UInt16, so just take the last four digits

                        break;

                    case 1:
                        throw new ArgumentException("SubWCRev.exe - Syntax error");

                    case 2:
                        throw new ArgumentException("SubWCRev.exe - File/folder not found");

                    case 3:
                        throw new ArgumentException("SubWCRev.exe - File open error");

                    case 4:
                        throw new ArgumentException("SubWCRev.exe - Memory allocation error");

                    case 5:
                        throw new ArgumentException("SubWCRev.exe - File read/write/size error");

                    case 6:
                        throw new ArgumentException("SubWCRev.exe - SVN error (is the working copy path correct?)");

                    case 7:
                        throw new ArgumentException("SubWCRev.exe - Local mods found (-n)");

                    case 8:
                        throw new ArgumentException("SubWCRev.exe - Mixed rev WC found (-m)");

                    case 9:
                        throw new ArgumentException("SubWCRev.exe - Output file already exists (-d)");

                    case 10:
                        throw new ArgumentException("SubWCRev.exe - the path is not a working copy or part of one");

                    default:
                        throw new ArgumentException("SubWCRev.exe - unknown exit code status (sorry!)");

	            }


            } catch (ArgumentException) {
                throw;
            } catch (Exception ex) {
                throw new ArgumentException("Problem running SubWCrev.exe", ex);
            } finally {
                // tidy up
                try {
                    File.Delete(tempFilename);
                } catch (Exception) { }

            }

            return svnRevisionNumber;
        }



        /// <summary>
        /// Writes an error message to the Console
        /// </summary>
        /// <param name="message"></param>
        private static void WriteErrorMessageToConsole(string message) {
            ConsoleColor originalColor = Console.ForegroundColor;

            Console.WriteLine("");
            Console.WriteLine("  WTV (When The Version) Automatic date-based version numbering for .Net projects");
            Console.WriteLine("  Andrew Freemantle - www.fatlemon.co.uk/wtv");
            Console.WriteLine("");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("    Error: " + message);

            Console.ForegroundColor = originalColor;
        }




        /// <summary>
        /// Prints usage instructions to the Console, and returns the int value for the ExitCode passed in
        /// </summary>
        private static int PrintUsage(ExitCode exitCode) {
            ConsoleColor originalColor = Console.ForegroundColor;

            Console.WriteLine("");
            Console.WriteLine("  WTV (When The Version) Automatic date-based version numbering for .Net projects");
            Console.WriteLine("  Andrew Freemantle - www.fatlemon.co.uk/wtv");
            Console.WriteLine("");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("    Error: " + exitCode.ToString().Replace("_", " "));

            Console.ForegroundColor = originalColor;
            Console.WriteLine("");
            Console.WriteLine("  Usage: WTV  \"file-in\"  \"file-out\"  [\"path to SubWCrev.exe\"  \"SVN working-copy-path\"]");
            Console.WriteLine("   \"file-in\"  can contain the following placeholders:");
            Console.WriteLine("     {DD}    - Day");
            Console.WriteLine("     {MM}    - Month");
            Console.WriteLine("     {YYYY}  - Year");
            Console.WriteLine("     {SVN}   - SubVersion revision (must specify the path to SubWCrev.exe and working copy path)");
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
            WTV_Problem_Doing_Replacements = 4,
            WTV_Problem_Getting_SVN_Revision_Number = 5

        }


    }
}
