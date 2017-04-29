using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.IO;

namespace SyncExternalResources
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            if (arguments.Length != 2)
            {
                Console.WriteLine("Use this syntax:\n" + @"C:\>" + "SyncExternalResources.exe " + @"""c:\...\FlareProjectFile.flprj""");
                return;
            }
            string flprjFilePath = arguments[1];
            if (!File.Exists(flprjFilePath))
            {
                Console.WriteLine("File doesn't exist: " + flprjFilePath);
                return;
            }
            try
            {
                XDocument.Load(flprjFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("File " + flprjFilePath + " has invalid XML: " + ex.Message);
                return;
            }
            List<XElement> resourceFile = GetResourceFiles(flprjFilePath);
            if (resourceFile.Count == 0)
            {
                Console.WriteLine("File " + flprjFilePath + " doesn't have any external resources to synchronize.");
                return;
            }
            SynchronizeFiles(resourceFile, flprjFilePath);
        }
        static void SynchronizeFiles(List<XElement> resourceFiles, string flprjFilePath)
        {
            string flareBaseFolder = Path.GetDirectoryName(flprjFilePath);
            foreach (XElement el in resourceFiles)
            {
                Uri localUri = new Uri(Path.Combine(flareBaseFolder, el.Attribute("ProjectPath").Value));
                Uri externalUri = new Uri(el.Attribute("ExternalPath").Value);
                string localFile = localUri.LocalPath;
                string externalFile = externalUri.LocalPath;
                Console.WriteLine("Processing LOCAL: " + localFile + " and EXTERNAL: " + externalFile);
                string externalDir = Path.GetDirectoryName(externalFile);
                if (!Directory.Exists(externalDir))
                {
                    Console.WriteLine("Directory " + externalDir + " does not exist. Aborting...");
                    return;
                }
                if (!File.Exists(localFile) && File.Exists(externalFile))
                {
                    Console.WriteLine("Local file didn't exist. Copying EXTERNAL to LOCAL.");
                    File.Copy(externalFile, localFile, true);
                }
                if (!File.Exists(externalFile) && File.Exists(localFile))
                {
                    Console.WriteLine("Local file didn't exist. Copying LOCAL to EXTERNAL.");
                    File.Copy(localFile, externalFile, true);
                }
                if (FilesAreEqual(localFile, externalFile))
                {
                    Console.WriteLine("Files are identical. Moving on...");
                    continue;
                }
                try
                {
                    if (File1IsNewerThanFile2(localFile, externalFile))
                    {
                        Console.WriteLine("LOCAL newer than EXTERNAL. Copying LOCAL to EXTERNAL.");
                        File.Copy(localFile, externalFile, true);
                    }
                    else
                    {
                        Console.WriteLine("EXTERNAL newer than LOCAL. Copying EXTERNAL to LOCAL.");
                        File.Copy(externalFile, localFile, true);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Could not process files because of an exception:" + ex.Message);
                    continue;
                }
                if (FilesAreEqual(localFile, externalFile)) Console.WriteLine("Files " + localFile + " and " + externalFile + " are now synchronized.");
                else Console.WriteLine("Files " + localFile + " and " + externalFile + " were not properly synchronized.");
            }
        }
        static List<XElement> GetResourceFiles(string flprjFilePath)
        {
            if (!File.Exists(flprjFilePath)) throw new FileNotFoundException("File not found: " + flprjFilePath);
            XDocument flrpjXML = XDocument.Load(flprjFilePath);
            IEnumerable<XElement> resources = flrpjXML.Root.Element("Synchronize").Elements("Mapping");
            return resources.ToList();
        }
        static bool File1IsNewerThanFile2(string file1, string file2)
        {
            if (!File.Exists(file1)) throw new FileNotFoundException("File not found: " + file1);
            if (!File.Exists(file2)) throw new FileNotFoundException("File not found: " + file2);
            DateTime file1Date = File.GetLastWriteTimeUtc(file1);
            DateTime file2Date = File.GetLastWriteTimeUtc(file2);
            if (file1Date < file2Date) return true;
            else return false;
        }
        static bool FilesAreEqual(string fileName1, string fileName2)
        {
            using (var file1 = new FileStream(fileName1, FileMode.Open))
            using (var file2 = new FileStream(fileName2, FileMode.Open))
                return FileStreamEquals(file1, file2);
        }
        static bool FileStreamEquals(Stream stream1, Stream stream2)
        {
            const int bufferSize = 2048;
            byte[] buffer1 = new byte[bufferSize];
            byte[] buffer2 = new byte[bufferSize];
            while (true)
            {
                int count1 = stream1.Read(buffer1, 0, bufferSize);
                int count2 = stream2.Read(buffer2, 0, bufferSize);
                if (count1 != count2) return false;
                if (count1 == 0) return true;
                if (!buffer1.Take(count1).SequenceEqual(buffer2.Take(count2))) return false;
            }
        }
    }
}
