using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace WeClean
{
    public static class DirectoryEnumerator
    {
        public static IEnumerable<FileInfo> WalkFiles(DirectoryInfo root, Random random)
        {
            FileInfo[] files;
            try
            {
                files = root.GetFiles("*.*");
            }
            catch (UnauthorizedAccessException)
            {
                // access denied
                yield break;
            }
            catch (DirectoryNotFoundException)
            {
                // directory removed since last access
                yield break;
            }

            foreach (FileInfo fi in files)
            {
                yield return fi;
            }

            DirectoryInfo[] subDirs = root.GetDirectories();
            subDirs.Shuffle(random);

            foreach (DirectoryInfo subDir in subDirs)
            {
                foreach (FileInfo subFile in WalkFiles(subDir, random))
                {
                    yield return subFile;
                }
            }
        }

        public static IEnumerable<DirectoryInfo> WalkDirectories(
            DirectoryInfo root,
            Random random)
        {
            DirectoryInfo[] subDirs;
            try
            {
                subDirs = root.GetDirectories();
            }
            catch (UnauthorizedAccessException)
            {
                // access denied
                yield break;
            }
            catch (DirectoryNotFoundException)
            {
                // directory removed since last access
                yield break;
            }

            subDirs.Shuffle(random);

            foreach (DirectoryInfo dir in subDirs)
            {
                yield return dir;

                foreach (DirectoryInfo subDir in WalkDirectories(dir, random))
                {
                    yield return subDir;
                }
            }
        }
    }
}