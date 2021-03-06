﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Security;
using System.Globalization;
using LibSWBF2.Types;
using LibSWBF2.Exceptions;
using LibSWBF2.WLD.Types;

namespace LibSWBF2.WLD {
    public class LYR {
        /// <summary>
        /// Name of this Layer
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// List of Objects this Layer contains
        /// </summary>
        public List<WorldObject> WorldObjects { get; private set; } = new List<WorldObject>();


        /// <summary>
        /// Loads a .lyr File
        /// </summary>
        /// <param name="path">The path to the .lyr File</param>
        /// <returns>A new LYR Instance</returns>
        /// <exception cref="ArgumentException">Path given is not valid</exception>
        /// <exception cref="EndOfDataException">Unexpected end of Data</exception>
        /// <exception cref="FileNotFoundException">File could not be found</exception>
        /// <exception cref="InsufficientPermissionsException">Insufficient Permissions</exception>
        /// <exception cref="IOException">Read / Write Error</exception>
        public static LYR LoadFromFile(string path) {
            StreamReader reader = null;
            string fileContent = "";

            try {
                reader = new StreamReader(path, Encoding.ASCII, false);
                fileContent = reader.ReadToEnd();

            } catch (ArgumentException ex) {
                Log.Add("Path given is not valid!", LogType.Error);
                throw new ArgumentException("Path given is not valid!", ex);
            } catch (NotSupportedException ex) {
                Log.Add("Path given is not valid!", LogType.Error);
                throw new ArgumentException("Path given is not valid!", ex);
            } catch (PathTooLongException ex) {
                Log.Add("Path given is not valid!", LogType.Error);
                throw new ArgumentException("Path given is not valid!", ex);
            } catch (UnauthorizedAccessException ex) {
                Log.Add("Insufficient Permissions!", LogType.Error);
                throw new InsufficientPermissionsException("Insufficient Permissions!", ex);
            } catch (SecurityException ex) {
                Log.Add("Insufficient Permissions!", LogType.Error);
                throw new InsufficientPermissionsException("Insufficient Permissions!", ex);
            } finally {
                if (reader != null)
                    reader.Close();
            }

            LYR lyr = LoadFromString(fileContent);
            lyr.Name = new FileInfo(path).Name.Replace(".lyr", "");

            return lyr;
        }

        /// <summary>
        /// Creates a new LYR instance from given string content. 
        /// </summary>
        /// <param name="content">The string to grab the information from</param>
        /// <returns>A new LYR Instance</returns>
        public static LYR LoadFromString(string content) {
            Log.Add(content, LogType.Info);

            LYR lyr = new LYR {
                Name = "New Layer"
            };

            //Regex for one Object Block
            Regex objectEntry = new Regex(
                // Object("objectName", "mshName", 666)
                @"Object\([""]([A-Za-z0-9_-]+)[""],\s*[""]([A-Za-z0-9_-]+)[""],\s*-?[0-9]+\)" +
                // {
                @"\s*\{" +
                // ChildRotation(-1.000, 0.000, 1.000, 666.666);
                @"\s*ChildRotation\((-?[0-9]+\.[0-9]+),\s*(-?[0-9]+\.[0-9]+),\s*(-?[0-9]+\.[0-9]+),\s*(-?[0-9]+\.[0-9]+)\);" +
                // ChildPosition(-1.000, 1.200, 666.666);
                @"\s*ChildPosition\((-?[0-9]+\.[0-9]+),\s*(-?[0-9]+\.[0-9]+),\s*(-?[0-9]+\.[0-9]+)\);"

                //interpret whole string as a single line (thus allowing matching across more than one line)
                , RegexOptions.Singleline);

            MatchCollection matches = objectEntry.Matches(content);

            foreach (Match match in matches) {
                if (match.Groups.Count == 10) {
                    WorldObject wldobj = new WorldObject();
                    wldobj.name = match.Groups[1].Value;
                    wldobj.meshName = match.Groups[2].Value;

                    wldobj.rotation = new Vector4(
                        Convert.ToSingle(match.Groups[3].Value, CultureInfo.InvariantCulture.NumberFormat),
                        Convert.ToSingle(match.Groups[4].Value, CultureInfo.InvariantCulture.NumberFormat),
                        Convert.ToSingle(match.Groups[5].Value, CultureInfo.InvariantCulture.NumberFormat),
                        Convert.ToSingle(match.Groups[6].Value, CultureInfo.InvariantCulture.NumberFormat)
                    );

                    wldobj.position = new Vector3(
                        Convert.ToSingle(match.Groups[7].Value, CultureInfo.InvariantCulture.NumberFormat),
                        Convert.ToSingle(match.Groups[8].Value, CultureInfo.InvariantCulture.NumberFormat),
                        Convert.ToSingle(match.Groups[9].Value, CultureInfo.InvariantCulture.NumberFormat)
                    );

                    lyr.WorldObjects.Add(wldobj);
                } else {
                    Log.Add("Number of found matches does not match! should be 10, is " + match.Groups.Count, LogType.Error);
                }
            }

            return lyr;
        }
    }
}
