using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Diagnostics;

namespace ffmpeg_qualityCompare
{
    class Program
    {
        static double average = 0;
        static double vifScore = 0;
        static double fileAverage = 0;
        static int bitdepth = 8;
        static List<string> Filenames_and_quality = new List<string>(); // the list where every piece of information is stored. TODO: rename to Filenames_Size_and_Quality


        static string compareBatFilename = "0-compare.bat";

        static void SetBitDepth(int depth)
        { bitdepth = depth; }
        static int GetBitDepth() { return bitdepth; }


        static List<string> CreateBatContentList(string compareFile, string referenceFile)
        {
            List<string> batList = new List<string>();

            string compareNoExt = compareFile.Substring(0, (compareFile.Length - 4));
            string compareWithExtension = compareFile.Substring(compareFile.LastIndexOf("\\") + 1);
            string compareCorrect = compareWithExtension.Substring(0, compareWithExtension.Length - 4);

            string referenceNoExt = referenceFile.Substring(0, (referenceFile.Length - 4));
            string referenceWithExtension = referenceFile.Substring(referenceFile.LastIndexOf("\\") + 1);
            string referenceCorrect = referenceWithExtension.Substring(0, referenceWithExtension.Length - 4);

            if (compareCorrect == referenceCorrect)
            {
                return batList;
            }

            //string[] algoArr = { "ssim", "psnr", "identity", "vif", "libvmaf", "msad", "corr" }; // Every option ffmpeg has. 
            string[] algoArr = { "ssim", "psnr", "vif", "msad", "corr" }; // The relevant ones that is built in.

            for (int algoInt = 0; algoInt < algoArr.Length; algoInt++)
            {
                string ffStr = "ffmpeg -i " + "\"" + compareWithExtension + "\"" + " -i " + "\"" + referenceWithExtension + "\"" + " -lavfi " + algoArr[algoInt];
                string txtStr = " -f null - 2> " + "\"" + compareCorrect + "_" + referenceCorrect + "_" + algoArr[algoInt] + ".txt" + "\"";

                // Old libvmaf 
                /*
                 * if (algoArr[algoInt] == "libvmaf")
                {
                    // specific code for libvmaf. The model path always needs to be escaped: "libvmaf=model_path=vmaf_v0.6.1.json "
                    ffStr = ffStr + "=model_path=vmaf_v0.6.1.json";
                    ffStr = ffStr + txtStr;

                }
                 * 
                 * 
                 */

                ffStr = ffStr + txtStr;


                batList.Add(ffStr);

            }
            return batList;

        }

        static void filenamegen(string filename)
        {
            File.WriteAllText(filename, filename);
        }
        static void Filegen(string compareFile, string referenceFile, bool everyFile)
        {


            // This will output a .bat file that compares the ground gruth files to the generated files from ffmpeg and swrescale.

            //ffmpeg -i main.mpg -i ref.mpg -lavfi ssim -f null -
            //ffmpeg -i main.mpg -i ref.mpg -lavfi psnr -f null -
            //ffmpeg -i main.mpg -i ref.mpg -lavfi identity -f null -
            //ffmpeg -i main.mpg -i ref.mpg -lavfi vif -f null -
            //ffmpeg -i main.mpg -i ref.mpg -lavfi libvmaf -f null - // not used as unreliable
            //ffmpeg -i main.mpg -i ref.mpg -lavfi msad -f null -
            //ffmpeg -i main.mpg -i ref.mpg -lavfi corr -f null -


            List<string> ffmpegBatList = new List<string>();

            if (everyFile == true)
            {
                foreach (var COMPAREFileFromDir in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.mov"))
                {
                    ffmpegBatList.AddRange(CreateBatContentList(COMPAREFileFromDir, referenceFile));

                }
                foreach (var COMPAREFileFromDir in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.mp4"))
                {
                    ffmpegBatList.AddRange(CreateBatContentList(COMPAREFileFromDir, referenceFile));

                }
                foreach (var COMPAREFileFromDir in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.mkv"))
                {
                    ffmpegBatList.AddRange(CreateBatContentList(COMPAREFileFromDir, referenceFile));

                }
            }
            else
            {
                ffmpegBatList.AddRange(CreateBatContentList(compareFile, referenceFile));

            }

            //filenamegen(compareFile);
            File.WriteAllLines(compareBatFilename, ffmpegBatList);

        }

        static long GetFileSize(string fileName)
        {
            FileInfo fi = new FileInfo(fileName);
            return fi.Length;
        }

        static void WriteResultFile(int numberOfTxt, List<string> Filenames_and_quality)
        {
            try
            {
                StreamWriter sw = new StreamWriter("Algo_qual.txt");
                double actualAvg = average / numberOfTxt;

                sw.WriteLine("Average of all the algorithms: " + actualAvg.ToString());
                sw.WriteLine();
                foreach (var line in Filenames_and_quality)
                {
                    sw.WriteLine(line);
                }

                sw.Close();
            }
            catch (Exception)
            {

            }

        }
        static void WriteResult()
        {
            int numberOfTxt = 0;
            double testfileResultDouble = 0;
            string resultStr = "";

            foreach (var file in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.txt"))
            {
                string FilenameWithExtention = file.Substring(file.LastIndexOf("\\") + 1);
                string FileNameNoExt = FilenameWithExtention.Substring(0, FilenameWithExtention.Length - 4);
                var lines = File.ReadLines(file);
                SetBitDepth(8);

                // find bit depth
                foreach (var line in lines)
                {
                    if (line.Contains("p10le"))
                    {
                        SetBitDepth(10);
                    }
                }


                // write results to list
                foreach (var line in lines)
                {
                    //if (line.Contains("VMAF score") || line.Contains("Parsed_msad_0") || line.Contains("Parsed_psnr_0")
                    //  || line.Contains("Parsed_ssim_0") || line.Contains("VIF scale=")||line.Contains("Parsed_corr") ||line.Contains("Parsed_identity"))
                    if (line.Contains("Parsed_corr") || line.Contains("Parsed_msad_0") || line.Contains("Parsed_psnr_0") || line.Contains("Parsed_ssim_0")
                        || line.Contains("VIF scale="))
                    {
                        resultStr = ReadResult(line, FileNameNoExt, GetBitDepth());
                        if (resultStr.Length > 0)
                        {
                            Filenames_and_quality.Add(resultStr);
                            numberOfTxt++;
                            if (line.Contains("VIF scale="))
                            {
                                string actualfilename = FileNameNoExt.Substring(0, FileNameNoExt.Length - 4); ; // TODO: needs to be changed to the actual thing


                                testfileResultDouble = fileAverage / 5; // TODO: Actual number of test file results.

                                Filenames_and_quality.Add(""); // empty line
                                Filenames_and_quality.Add(actualfilename + " average: " + testfileResultDouble + " ");

                                Filenames_and_quality.Add(""); // empty line
                                fileAverage = 0;

                            }
                        }


                    }

                }
            }

            WriteResultFile(numberOfTxt, Filenames_and_quality);



        }

        //static string ReadResult(string line, string filenameNoExt) // Every option as of 26 april 2024. The version below this one has all the non-valid removed
        //{
        //    string resultStr = "";


        //    if (line.Contains("VMAF score"))
        //    {

        //        // [libvmaf @ 0000020f1a8d39c0] VMAF score: 98.979744

        //        string VMAFScoreAll = line.Substring(line.IndexOf("VMAF score") + 12);

        //        string VMAFScore = "";
        //        if (VMAFScoreAll.Length == 9)
        //        {
        //            VMAFScore = line.Substring(line.IndexOf("VMAF score") + 12).Substring(0, 9); // The score is 2 digits a period then 6 digits => 9 characters
        //        }
        //        else
        //        {
        //            VMAFScore = line.Substring(line.IndexOf("VMAF score") + 12).Substring(0, 8); //The score is 1 digit a period then 6 digits => 9 characters
        //        }

        //        VMAFScore = VMAFScore.Replace(".", ",");
        //        float VMAFFloat = float.Parse(VMAFScore);
        //        VMAFFloat = VMAFFloat * 1.01f; // Average amount the VMAF score is too low based on comparing two videos against itself. 

        //        string Filename_and_fpsStr = filenameNoExt + ";" + VMAFFloat;
        //        average = average + double.Parse(VMAFScore);

        //        //resultStr = Filename_and_fpsStr + ";" + getFileSize(filenameWithExt).ToString(); // Takes the size from the txt file, not the test file
        //        resultStr = Filename_and_fpsStr;
        //    }
        //    else if (line.Contains("Parsed_msad_0"))
        //    {

        //        double actualValue = 0;
        //        string Filename_and_fpsStr = "";
        //        if (line.Contains("average:0.000000"))
        //        //[Parsed_msad_0 @ 000002c8a7b03700] msad R:0.000000 G:0.000000 B:0.000000 A:0.000000 average:0.000000 min:0.000000 max:0.000000
        //        {
        //            actualValue = 100;
        //        }
        //        else
        //        //[Parsed_msad_0 @ 000001fa5e6a8ec0] msad R:0.007541 G:0.008110 B:0.007207 average:0.007620 min:0.001948 max:0.012130
        //        //[Parsed_msad_0 @ 0000019b13874d00] msad Y:0.002856 U: 0.002033 V: 0.001891 average: 0.002260 min: 0.001464 max: 0.002713
        //        {
        //            string ParseResult = line.Substring(line.IndexOf("average:") + 8, 8);
        //            ParseResult = ParseResult.Replace(".", ",");
        //            actualValue = 100 - double.Parse(ParseResult);

        //        }

        //        Filename_and_fpsStr = filenameNoExt + ";" + actualValue;
        //        average = average + actualValue;


        //        //resultStr = Filename_and_fpsStr + ";" + getFileSize(filenameWithExt).ToString(); // Takes the size from the txt file, not the test file
        //        resultStr = Filename_and_fpsStr;
        //    }
        //    else if (line.Contains("Parsed_psnr_0"))
        //    {

        //        //[Parsed_psnr_0 @ 000001d959af5180] PSNR r:inf g:inf b:inf a:inf average:inf min:inf max:inf
        //        if (line.Contains("inf"))
        //        {
        //            string fps = "100";
        //            string Filename_and_fpsStr = filenameNoExt + ";" + fps;
        //            average = average + double.Parse(fps);

        //            //resultStr = Filename_and_fpsStr + ";" + getFileSize(filenameWithExt).ToString(); // Takes the size from the txt file, not the test file
        //            resultStr = Filename_and_fpsStr;
        //        }
        //        else
        //        //[Parsed_psnr_0 @ 0000020e66888480] PSNR y:32.858832 u:43.313200 v:41.155595 average:36.702531 min:36.102411 max:37.343557
        //        //[Parsed_psnr_0 @ 000001aa9aebef00] PSNR y:33.029119 u:44.250376 v:42.139314 average:37.014945 min:36.360319 max:37.750726
        //        {
        //            //for 8 bit, the max PSNR is 48,164799 (20*log(256), it's higher for 10 bit, but I clamp it at 100, even for 10 bit videos. 
        //            //TODO FIXME: One division for 8 and one for 10 bit
        //            // meaning that any value is divided by 0.6 to get a normalized value in the 0-100 range. 
        //            string fps = line.Substring(84).Substring(0, 9);
        //            fps = fps.Replace(".", ",");
        //            double actualValue = double.Parse(fps) / 0.48164799;
        //            if (actualValue > 100)
        //            {
        //                actualValue = 100;
        //            }

        //            string Filename_and_fpsStr = filenameNoExt + ";" + actualValue;
        //            average = average + actualValue;

        //            //resultStr = Filename_and_fpsStr + ";" + getFileSize(filenameWithExt).ToString(); // Takes the size from the txt file, not the test file
        //            resultStr = Filename_and_fpsStr;

        //        }


        //    }
        //    else if (line.Contains("Parsed_ssim_0"))
        //    {

        //        double resultDouble = 0;
        //        string Filename_and_fpsStr = "";
        //        if (line.Contains("inf")) // identical
        //        {
        //            resultDouble = 100;

        //        }
        //        else // different
        //        {
        //            //[Parsed_ssim_0 @ 0000017c60568480] SSIM Y:0.988230 (19.292292) U:0.994786 (22.828061) V:0.992659 (21.342683) All:0.991892 (20.910733)
        //            //[Parsed_ssim_0 @ 0000028736148ec0] SSIM R:0.967100 (14.827983) G:0.968149 (14.968729) B:0.971683 (15.479500) All:0.968977 (15.083166)

        //            resultDouble = double.Parse(line.Substring(line.IndexOf("All:") + 4, 8).Replace(".", ",")) * 100;


        //        }
        //        Filename_and_fpsStr = filenameNoExt + ";" + resultDouble;
        //        average = average + resultDouble;


        //        //resultStr = Filename_and_fpsStr + ";" + getFileSize(filenameWithExt).ToString(); // Takes the size from the txt file, not the test file
        //        resultStr = Filename_and_fpsStr;


        //    }
        //    else if (line.Contains("VIF scale="))
        //    {


        //        //[Parsed_vif_0 @ 000001aa505e5180] VIF scale=0 average:1.000000 min:1.000000: max:1.000000
        //        //[Parsed_vif_0 @ 000001f74d145180] VIF scale=1 average:1.000000 min:1.000000: max:1.000000
        //        //[Parsed_vif_0 @ 000001f74d145180] VIF scale=2 average:1.000000 min:1.000000: max:1.000000
        //        //[Parsed_vif_0 @ 000001f74d145180] VIF scale=3 average:1.000000 min:1.000000: max:1.000000

        //        // cut to average number to float
        //        // put AvgF to vifscore 

        //        double vifScaleDouble = double.Parse(line.Substring(54).Substring(0, 8)) / 10000d; ;

        //        // 17+9+5+3 = 34. So actually the final score is (VIF scale=0) /(17/34)+(VIF scale=1)/(9/34)+(VIF scale=2)/(5/34)+(VIF scale=3)/(3/34).
        //        if (line.Contains("VIF scale=0"))
        //        {

        //            double vifScale0Double = vifScaleDouble * (double)(17d / 34d);
        //            vifScore = vifScore + vifScale0Double;

        //        }
        //        if (line.Contains("VIF scale=1"))
        //        {
        //            double vifScale1Double = vifScaleDouble * (double)(9d / 34d);
        //            vifScore = vifScore + vifScale1Double;
        //        }
        //        if (line.Contains("VIF scale=2"))
        //        {
        //            double vifScale2Double = vifScaleDouble * (double)(5d / 34d);
        //            vifScore = vifScore + vifScale2Double;
        //        }
        //        if (line.Contains("VIF scale=3"))
        //        {
        //            double vifScale3Double = vifScaleDouble * (double)(3d / 34d);
        //            vifScore = vifScore + vifScale3Double;



        //            average = average + vifScore;

        //            string Filename_and_vifStr = filenameNoExt + ";" + vifScore.ToString();
        //            //resultStr = Filename_and_vifStr + ";" + getFileSize(filenameWithExt).ToString(); // Takes the size from the txt file, not the test file
        //            resultStr = Filename_and_vifStr;
        //            vifScore = 0;
        //        }


        //    }
        //    else if (line.Contains("Parsed_corr_0"))
        //    {

        //        double resultDouble = 0;
        //        string Filename_and_fpsStr = "";

        //        //[Parsed_corr_0 @ 000002885d5fe9c0] corr Y:1.000000 U:1.000000 V:1.000000 average:1.000000 min:1.000000 max:1.000000

        //        resultDouble = double.Parse(line.Substring(line.IndexOf("average:") + 8, 8).Replace(".", ",")) * 100;



        //        Filename_and_fpsStr = filenameNoExt + ";" + resultDouble;
        //        average = average + resultDouble;


        //        //resultStr = Filename_and_fpsStr + ";" + getFileSize(filenameWithExt).ToString(); // Takes the size from the txt file, not the test file
        //        resultStr = Filename_and_fpsStr;

        //    }
        //    //else if (line.Contains("Parsed_identity_0"))  // Identity works perfectly as code. as of 6 april 2024)
        //    // HOWEVER!
        //    // The Identity algorithm extensively punishes difference and as such will skew any results far downwards. 
        //    // As an exmaple: Visually identical videos (h264 high 4:2:2 L6.0, yuv422p10le, 2560x1440, QP24 vs QP0):
        //    // corr;99,9952. msad;99,999113. psnr;100. ssim;99,8938. vif;96,4355235294118. Identity: 41,4281. 
        //    // I can not stress enough how massive that difference is in Identity compared to the rest. 
        //    // As such I have disabled it, but left it as a possibility for the next user to re-enable
        //    //{

        //    //    double resultDouble = 0;
        //    //    string Filename_and_fpsStr = "";

        //    //    //[Parsed_corr_0 @ 000002885d5fe9c0] corr Y:1.000000 U:1.000000 V:1.000000 average:1.000000 min:1.000000 max:1.000000

        //    //    resultDouble = double.Parse(line.Substring(line.IndexOf("average:") + 8, 8).Replace(".", ",")) * 100;



        //    //    Filename_and_fpsStr = filename + ";" + resultDouble;
        //    //    average = average + resultDouble;


        //    //    resultStr = Filename_and_fpsStr;


        //    //}
        //    else
        //    {

        //    }
        //    return resultStr;
        //}

        static string ReadResult(string line, string filenameNoExt, int bitdepth) // read result from ffmpeg output text file
        {
            string resultStr = "";
            if (line.Contains("Parsed_corr_0") && (line.Contains("average:")))
            {

                double resultDouble = 0;
                string Filename_and_fpsStr = "";


                //[Parsed_corr_0 @ 000002885d5fe9c0] corr Y:1.000000 U:1.000000 V:1.000000 average:1.000000 min:1.000000 max:1.000000
                //[Parsed_corr_0 @ 000002a55ef0ee80] corr Y:0.999749 U:0.999297 V:0.999555 average:0.999534 min:0.895737 max:1.000000

                resultDouble = double.Parse(line.Substring(line.IndexOf("average:") + 8, 8).Replace(".", ",")) * 100;



                Filename_and_fpsStr = filenameNoExt + ";" + resultDouble;
                average = average + resultDouble;
                fileAverage = fileAverage + resultDouble;

                //resultStr = Filename_and_fpsStr + ";" + getFileSize(filenameWithExt).ToString(); // Takes the size from the txt file, not the test file
                resultStr = Filename_and_fpsStr;

            }

            else if (line.Contains("Parsed_msad_0") && (line.Contains("average:")))
            {

                double resultDouble = 0;
                string Filename_and_fpsStr = "";
                if (line.Contains("average:0.000000"))
                //[Parsed_msad_0 @ 000002c8a7b03700] msad R:0.000000 G:0.000000 B:0.000000 A:0.000000 average:0.000000 min:0.000000 max:0.000000
                {
                    resultDouble = 100;
                }
                else
                //[Parsed_msad_0 @ 000001fa5e6a8ec0] msad R:0.007541 G:0.008110 B:0.007207 average:0.007620 min:0.001948 max:0.012130
                //[Parsed_msad_0 @ 0000019b13874d00] msad Y:0.002856 U: 0.002033 V: 0.001891 average: 0.002260 min: 0.001464 max: 0.002713
                {
                    string ParseResult = line.Substring(line.IndexOf("average:") + 8, 8);
                    ParseResult = ParseResult.Replace(".", ",");
                    resultDouble = 100 - double.Parse(ParseResult);

                }

                Filename_and_fpsStr = filenameNoExt + ";" + resultDouble;
                average = average + resultDouble;
                fileAverage = fileAverage + resultDouble;

                //resultStr = Filename_and_fpsStr + ";" + getFileSize(filenameWithExt).ToString(); // Takes the size from the txt file, not the test file
                resultStr = Filename_and_fpsStr;
            }
            else if (line.Contains("Parsed_psnr_0") && (line.Contains("average:")))
            {

                //[Parsed_psnr_0 @ 000001d959af5180] PSNR r:inf g:inf b:inf a:inf average:inf min:inf max:inf
                if (line.Contains("inf"))
                {
                    string PSNR_infStr = "100";
                    string Filename_and_fpsStr = filenameNoExt + ";" + PSNR_infStr;
                    average = average + double.Parse(PSNR_infStr);
                    fileAverage = fileAverage + double.Parse(PSNR_infStr);

                    //resultStr = Filename_and_fpsStr + ";" + getFileSize(filenameWithExt).ToString(); // Takes the size from the txt file, not the test file
                    resultStr = Filename_and_fpsStr;
                }
                else
                //[Parsed_psnr_0 @ 0000020e66888480] PSNR y:32.858832 u:43.313200 v:41.155595 average:36.702531 min:36.102411 max:37.343557
                //[Parsed_psnr_0 @ 000001aa9aebef00] PSNR y:33.029119 u:44.250376 v:42.139314 average:37.014945 min:36.360319 max:37.750726
                {
                    // for 8 bit, the max PSNR is 48,164799 (20*log10(256).
                    // for 10 bit, the max PSNR is 60,205999 (20*log10(1024).

                    // TODO FIXME: One division for 8 and one for 10 bit
                    // meaning that any value is divided by 0.6 to get a normalized value in the 0-100 range. 
                    string ParseResult = line.Substring(line.IndexOf("average:") + 8, 9);

                    ParseResult = ParseResult.Replace(".", ",");
                    double actualValue = double.Parse(ParseResult);

                    if (bitdepth == 8)
                    {
                        actualValue = actualValue / 0.48164799;
                        if (actualValue > 100)
                        {
                            actualValue = 100;
                        }
                    }
                    else
                    {
                        actualValue = actualValue / 0.60205999;
                        if (actualValue > 100)
                        {
                            actualValue = 100;
                        }

                    }


                    string Filename_and_fpsStr = filenameNoExt + ";" + actualValue;
                    average = average + actualValue;
                    fileAverage = fileAverage + actualValue;
                    //resultStr = Filename_and_fpsStr + ";" + getFileSize(filenameWithExt).ToString(); // Takes the size from the txt file, not the test file
                    resultStr = Filename_and_fpsStr;

                }


            }
            else if (line.Contains("Parsed_ssim_0") && (line.Contains("All:")))
            {

                double resultDouble = 0;
                string Filename_and_fpsStr = "";
                if (line.Contains("inf")) // identical
                {
                    resultDouble = 100;

                }
                else // different
                {
                    //[Parsed_ssim_0 @ 0000017c60568480] SSIM Y:0.988230 (19.292292) U:0.994786 (22.828061) V:0.992659 (21.342683) All:0.991892 (20.910733)
                    //[Parsed_ssim_0 @ 0000028736148ec0] SSIM R:0.967100 (14.827983) G:0.968149 (14.968729) B:0.971683 (15.479500) All:0.968977 (15.083166)

                    resultDouble = double.Parse(line.Substring(line.IndexOf("All:") + 4, 8).Replace(".", ",")) * 100;


                }
                Filename_and_fpsStr = filenameNoExt + ";" + resultDouble;
                average = average + resultDouble;
                fileAverage = fileAverage + resultDouble;

                //resultStr = Filename_and_fpsStr + ";" + getFileSize(filenameWithExt).ToString(); // Takes the size from the txt file, not the test file
                resultStr = Filename_and_fpsStr;


            }
            else if (line.Contains("VIF scale=") && (line.Contains("average:")))
            {


                //[Parsed_vif_0 @ 000001aa505e5180] VIF scale=0 average:1.000000 min:1.000000: max:1.000000
                //[Parsed_vif_0 @ 000001f74d145180] VIF scale=1 average:1.000000 min:1.000000: max:1.000000
                //[Parsed_vif_0 @ 000001f74d145180] VIF scale=2 average:1.000000 min:1.000000: max:1.000000
                //[Parsed_vif_0 @ 000001f74d145180] VIF scale=3 average:1.000000 min:1.000000: max:1.000000

                // cut to average number to float
                // put AvgF to vifscore 

                double vifScaleDouble = double.Parse(line.Substring(54).Substring(0, 8)) / 10000d; ;

                // 17+9+5+3 = 34. So actually the final score is (VIF scale=0) /(17/34)+(VIF scale=1)/(9/34)+(VIF scale=2)/(5/34)+(VIF scale=3)/(3/34).
                if (line.Contains("VIF scale=0"))
                {

                    double vifScale0Double = vifScaleDouble * (double)(17d / 34d);
                    vifScore = vifScore + vifScale0Double;

                }
                if (line.Contains("VIF scale=1"))
                {
                    double vifScale1Double = vifScaleDouble * (double)(9d / 34d);
                    vifScore = vifScore + vifScale1Double;
                }
                if (line.Contains("VIF scale=2"))
                {
                    double vifScale2Double = vifScaleDouble * (double)(5d / 34d);
                    vifScore = vifScore + vifScale2Double;
                }
                if (line.Contains("VIF scale=3"))
                {
                    double vifScale3Double = vifScaleDouble * (double)(3d / 34d);
                    vifScore = vifScore + vifScale3Double;



                    average = average + vifScore;
                    fileAverage = fileAverage + vifScore;

                    string Filename_and_vifStr = filenameNoExt + ";" + vifScore.ToString();
                    //resultStr = Filename_and_vifStr + ";" + getFileSize(filenameWithExt).ToString(); // Takes the size from the txt file, not the test file
                    resultStr = Filename_and_vifStr;
                    vifScore = 0;
                }


            }

            return resultStr;
        }

        static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                if (args[0] == "avg")
                {
                    // Write the average of the results, as in the last function here
                    WriteResult();
                    return;
                }
            }


            else if (args.Length == 0)
            {
                Console.WriteLine("Syntax can also be ffmpeg-qualityCompare avg, for creating the average after compare is done");



                Console.WriteLine("Input REFERENCE filename, if empty, write a sample compare file. Confirm with Return");
                string referenceFileCMD = Console.ReadLine();
                if (referenceFileCMD.Length == 0)
                {
                    Filegen("COMPARE", "REFERENCE", false);
                }
                if (referenceFileCMD.Length < 4)
                {
                    Console.WriteLine("REFERENCE must have a full extension, eg .mov");
                    return;
                }
                else
                {
                    Console.WriteLine("Do you want to compare against every mov|mp4|mkv file in the current directory? Y/N and Return");
                    if (Console.ReadLine() == "y")
                    {
                        Filegen("", referenceFileCMD, true);
                    }
                    else
                    {
                        Console.WriteLine("Write COMPARE filename");
                        String compareFileCMD = Console.ReadLine();
                        if (compareFileCMD.Length == 0)
                        {
                            Console.WriteLine("COMPARE can not be empty");
                            return;
                        }
                        else if (compareFileCMD.Length < 4)
                        {
                            Console.WriteLine("COMPARE must have a full extension, eg .mov");
                            return;
                        }
                        else
                        {
                            Filegen(compareFileCMD, referenceFileCMD, false);
                        }

                    }

                }

            }


            Console.WriteLine("Want to execute the .bat file? Y/N with enter");
            if (Console.ReadLine() == "y")
            {
                Process p = new Process();
                p.StartInfo.FileName = compareBatFilename;
                p.Start();
                p.WaitForExit();



                // wait for the processing to stop before moving forward

            }
            else
            {

            }

            Console.WriteLine("Want to calculate the average scores? Y/N with enter");
            if (Console.ReadLine() == "y")
            {
                WriteResult();
            }

            else
            {

            }




        }
    }
}
