using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static scalagen2fssp_hifi_calibrator.MainForm;

namespace scalagen2fssp_hifi_calibrator
{
    public partial class MainForm : Form
    {
        void button_RunModel_InfiniteSurfaces(object sender, EventArgs e)
        {
            SetConstants();

            //infinite horizontal surface, asphalt
            if (checkBox_snippetsFolder_Surf_1.Checked)
            {
                string foldername = textBox_snippetsFolder_Surf_1_Smart.Text;
                if (sensorVariant == 0) foldername = textBox_snippetsFolder_Surf_1_Smart.Text;
                if (sensorVariant == 1) foldername = textBox_snippetsFolder_Surf_1_Slim.Text;
                if (sensorVariant == 2) foldername = textBox_snippetsFolder_Surf_1_Satellite.Text;

                string matID_Surf_Txt = textBox_snippetsMatID_Surf_1_Smart.Text;
                if (sensorVariant == 0) matID_Surf_Txt = textBox_snippetsMatID_Surf_1_Smart.Text;
                if (sensorVariant == 1) matID_Surf_Txt = textBox_snippetsMatID_Surf_1_Slim.Text;
                if (sensorVariant == 2) matID_Surf_Txt = textBox_snippetsMatID_Surf_1_Satellite.Text;

                int matID = int.Parse(matID_Surf_Txt);
                double[] fixNs = new double[3] { -1, 0, 0 };

                string slantRange_Text = sensorVariant == 0 ? textBox_slantRange_Surf_1_Smart.Text : textBox_slantRange_Surf_1_Slim.Text;
                double fixedSlantRange = double.Parse(slantRange_Text, NumberStyles.Float, CultureInfo.InvariantCulture);
                bool fixSlantRange = sensorVariant == 0 ? checkBox_fixSlantrange_Surf_1_Smart.Checked : checkBox_fixSlantrange_Surf_1_Slim.Checked;

                ReadSnippetsFolderAndShowResultsWithInfiniteSurfaces(foldername, chart_Surf_1_PEAK, chart_Surf_1_AREA, chart_Surf_1_WIDTH, chart_Surf_1_DIST,
                    matID, fixNs, fixedSlantRange, fixSlantRange, 1);
            }

            if (checkBox_snippetsFolder_Surf_2.Checked)
            {
                string foldername = textBox_snippetsFolder_Surf_2_Smart.Text;
                if (sensorVariant == 0) foldername = textBox_snippetsFolder_Surf_2_Smart.Text;
                if (sensorVariant == 1) foldername = textBox_snippetsFolder_Surf_2_Slim.Text;
                if (sensorVariant == 2) foldername = textBox_snippetsFolder_Surf_2_Satellite.Text;

                string matID_Surf_Txt = textBox_snippetsMatID_Surf_2_Smart.Text;
                if (sensorVariant == 0) matID_Surf_Txt = textBox_snippetsMatID_Surf_2_Smart.Text;
                if (sensorVariant == 1) matID_Surf_Txt = textBox_snippetsMatID_Surf_2_Slim.Text;
                if (sensorVariant == 2) matID_Surf_Txt = textBox_snippetsMatID_Surf_2_Satellite.Text;

                int matID = int.Parse(matID_Surf_Txt);
                
                string slantRange_Text = textBox_snippetsMatID_Surf_2_Smart.Text;
                if (sensorVariant == 0) slantRange_Text = textBox_slantRange_Surf_2_Smart.Text;
                if (sensorVariant == 1) slantRange_Text = textBox_slantRange_Surf_2_Slim.Text;
                if (sensorVariant == 2) slantRange_Text = textBox_slantRange_Surf_2_Satellite.Text;

                double fixedSlantRange = double.Parse(slantRange_Text, NumberStyles.Float, CultureInfo.InvariantCulture);
                
                bool fixSlantRange = checkBox_fixSlantrange_Surf_2_Smart.Checked;
                if (sensorVariant == 0) fixSlantRange = checkBox_fixSlantrange_Surf_2_Smart.Checked;
                if (sensorVariant == 1) fixSlantRange = checkBox_fixSlantrange_Surf_2_Slim.Checked;
                if (sensorVariant == 2) fixSlantRange = checkBox_fixSlantrange_Surf_2_Satellite.Checked;

                string tiltAngle_Text = textBox_tiltAngle_Surf_2_Smart.Text;
                if (sensorVariant == 0) tiltAngle_Text = textBox_tiltAngle_Surf_2_Smart.Text;
                if (sensorVariant == 1) tiltAngle_Text = textBox_tiltAngle_Surf_2_Slim.Text;
                if (sensorVariant == 2) tiltAngle_Text = textBox_tiltAngle_Surf_2_Satellite.Text;
                double tiltAngle_rad = Math.PI/180.0 * double.Parse(tiltAngle_Text, NumberStyles.Float, CultureInfo.InvariantCulture);

                double[] fixNs = new double[3] { -Math.Tan(tiltAngle_rad), 0, Math.Cos(tiltAngle_rad) };

                ReadSnippetsFolderAndShowResultsWithInfiniteSurfaces(foldername, chart_Surf_2_PEAK, chart_Surf_2_AREA, chart_Surf_2_WIDTH, chart_Surf_2_DIST,
                    matID, fixNs, fixedSlantRange, fixSlantRange, 1);
            }

        }

        void ReadSnippetsFolderAndShowResultsWithInfiniteSurfaces(
            string foldername,
            System.Windows.Forms.DataVisualization.Charting.Chart chart_PEAK,
            System.Windows.Forms.DataVisualization.Charting.Chart chart_AREA,
            System.Windows.Forms.DataVisualization.Charting.Chart chart_WIDTH,
            System.Windows.Forms.DataVisualization.Charting.Chart chart_DIST,
            int matID, double[] fixNs, double fixedSlantRange, bool fixSlantRange, float mirrorSide)
        {

            foreach (var series in chart_PEAK.Series) series.Points.Clear();
            foreach (var series in chart_AREA.Series) series.Points.Clear();
            foreach (var series in chart_WIDTH.Series) series.Points.Clear();

            // get the relevant Folder contents
            SnippetFolderContents snippetFolderContents = ReadSnippetsFolder(foldername, matID, fixNs, false);

            DebugBox.AppendText("snippetFolderContents.Snippet_File_List.Count: " + snippetFolderContents.Snippet_File_List.Count + "\n");

            bool showAllPoints = snippetFolderContents.Snippet_File_List.Count == 1;

            //REAL
            for (int i = 0; i < snippetFolderContents.Snippet_File_List.Count; i++)
            {
                // get average distance
                double distanceAverageReal = 0;
                for (int p = 0; p < snippetFolderContents.Snippet_File_List[i].Snippet_Point_List.Count; p++)
                {
                    Snippet_Point point = snippetFolderContents.Snippet_File_List[i].Snippet_Point_List[p];
                    if (point.distance > 0.1) distanceAverageReal += point.distance / snippetFolderContents.Snippet_File_List[i].Snippet_Point_List.Count;
                }

                var peakValues = new List<double>();
                var areaValues = new List<double>();
                var widthValues = new List<double>();
                var distDiffValues = new List<double>();
                var distanceValues = new List<double>();

                // collect data
                for (int p = 0; p < snippetFolderContents.Snippet_File_List[i].Snippet_Point_List.Count; p++)
                {
                    Snippet_Point point = snippetFolderContents.Snippet_File_List[i].Snippet_Point_List[p];
                    if (point.distance > 0.1)
                    {
                        peakValues.Add(point.PEAK);
                        areaValues.Add(point.AREA);
                        widthValues.Add(point.WIDTH);
                        distDiffValues.Add(point.distance - distanceAverageReal);
                        distanceValues.Add(point.distance);
                    }
                }

                if (distanceValues.Any() && showAllPoints)
                {
                    for (int p = 0; p < snippetFolderContents.Snippet_File_List[i].Snippet_Point_List.Count; p++)
                    {
                        Snippet_Point point = snippetFolderContents.Snippet_File_List[i].Snippet_Point_List[p];
                        if (point.distance > 0.1)
                        {
                            chart_PEAK.Series["Real"].Points.AddXY(point.distance, point.PEAK);
                            chart_AREA.Series["Real"].Points.AddXY(point.distance, point.AREA);
                            chart_WIDTH.Series["Real"].Points.AddXY(point.distance, point.WIDTH);
                            chart_DIST.Series["Real"].Points.AddXY(point.distance, point.distance - distanceAverageReal);
                        }
                    }
                }
                else if(distanceValues.Any())
                {
                    double avgDistance = distanceValues.Average();

                    Action<Chart, List<double>> plotStats = (chart, values) =>
                    {
                        if (values.Any())
                        {
                            double mean = values.Average();
                            double stdDev = Math.Sqrt(values.Sum(v => Math.Pow(v - mean, 2)) / values.Count);
                            chart.Series["Real"].Points.AddXY(avgDistance, mean);
                            chart.Series["Real"].Points.AddXY(avgDistance, mean + stdDev);
                            chart.Series["Real"].Points.AddXY(avgDistance, mean - stdDev);
                        }
                    };

                    plotStats(chart_PEAK, peakValues);
                    plotStats(chart_AREA, areaValues);
                    plotStats(chart_WIDTH, widthValues);
                    plotStats(chart_DIST, distDiffValues);
                }
            }

            //SIM
            for (int i_sfc = 0; i_sfc < snippetFolderContents.Snippet_File_List.Count; i_sfc++)
            {
                var snippetFile = snippetFolderContents.Snippet_File_List [i_sfc];
                var (allEchoSims_dist, allEchoSims_data, pointsPerLayerSim) = ProcessEchoSimulation(snippetFile, fixNs, fixedSlantRange, fixSlantRange, false);
                double distanceAverageSim = 0;
                int validEchoCount = 0;

                // Summate the distance and count no.of points
                for (int i = 0; i < allEchoSims_dist.Count; i++)
                {
                    if (allEchoSims_dist[i].RISING < 400)
                    {
                        distanceAverageSim += allEchoSims_dist[i].RISING;
                        validEchoCount++;
                    }
                }

                // Calculate the average
                if (validEchoCount > 0)
                {
                    distanceAverageSim /= validEchoCount;
                }

                // Plot the charts with the updated average value
                var simPeakValues = new List<double>();
                var simAreaValues = new List<double>();
                var simWidthValues = new List<double>();
                var simDistValues = new List<double>();
                var simRisingValues = new List<double>();

                for (int i = 0; i < allEchoSims_data.Count; i++)
                {
                    if (allEchoSims_data[i].RISING < 400)
                    {
                        simPeakValues.Add(allEchoSims_data[i].PEAK);
                        simAreaValues.Add(allEchoSims_data[i].AREA);
                        simWidthValues.Add(allEchoSims_data[i].WIDTH);
                        simDistValues.Add(allEchoSims_data[i].RISING - distanceAverageSim);
                        simRisingValues.Add(allEchoSims_data[i].RISING);
                    }
                }

                if (simRisingValues.Any() && showAllPoints)
                {
                    for (int i = 0; i < allEchoSims_data.Count; i++)
                    {
                        if (allEchoSims_data[i].RISING < 400)
                        {
                            // collect data using the finalized average
                            chart_PEAK.Series["Sim"].Points.AddXY(allEchoSims_data[i].RISING, allEchoSims_data[i].PEAK);
                            chart_AREA.Series["Sim"].Points.AddXY(allEchoSims_data[i].RISING, allEchoSims_data[i].AREA);
                            chart_WIDTH.Series["Sim"].Points.AddXY(allEchoSims_data[i].RISING, allEchoSims_data[i].WIDTH);
                            chart_DIST.Series["Sim"].Points.AddXY(allEchoSims_data[i].RISING, allEchoSims_data[i].RISING);
                        }
                    }
                }
                else if (simRisingValues.Any())
                {
                    double avgRising = simRisingValues.Average();

                    Action<Chart, List<double>> plotStats = (chart, values) =>
                    {
                        if (values.Any())
                        {
                            double mean = values.Average();
                            double stdDev = Math.Sqrt(values.Sum(v => Math.Pow(v - mean, 2)) / values.Count);
                            chart.Series["Sim"].Points.AddXY(avgRising, mean);
                            chart.Series["Sim"].Points.AddXY(avgRising, mean + stdDev);
                            chart.Series["Sim"].Points.AddXY(avgRising, mean - stdDev);
                        }
                    };

                    plotStats(chart_PEAK, simPeakValues);
                    plotStats(chart_AREA, simAreaValues);
                    plotStats(chart_WIDTH, simWidthValues);
                    plotStats(chart_DIST, simDistValues);
                }
            }            

            PCCommonFunctions.ResetAxesInChart(chart_PEAK, false, false);
            PCCommonFunctions.ResetAxesInChart(chart_AREA, false, false);
            PCCommonFunctions.ResetAxesInChart(chart_WIDTH, false, false);
            PCCommonFunctions.ResetAxesInChart(chart_DIST, false, false);
            chart_DIST.ChartAreas[0].AxisY.Minimum = -0.6;
            chart_DIST.ChartAreas[0].AxisY.Maximum = 0.6;

            //double hausdorffDistanceWIDTH = ComputeHausdorffDistance(chart_WIDTH.Series["Sim"], chart_AREA.Series["Real"]);
            //double hausdorffDistancePEAK = ComputeHausdorffDistance(chart_PEAK.Series["Sim"], chart_PEAK.Series["Real"]);
            //double hausdorffDistanceAREA = ComputeHausdorffDistance(chart_AREA.Series["Sim"], chart_AREA.Series["Real"]);


            //DebugBox.AppendText("hausdorffDistance PEAK, AREA, WIDTH: " + hausdorffDistancePEAK + "   " + hausdorffDistanceAREA + "   " + hausdorffDistanceWIDTH + "\n");
        }

        SnippetFolderContents ReadSnippetsFolder(string foldername, int matID, double[] fixNs, bool fetchLayerFromFile)
        {
            var snippetFolderContents = new SnippetFolderContents();

            // handle .txt files (existing behavior)
            string[] txtFiles = Directory.GetFiles(foldername, "*.txt", SearchOption.TopDirectoryOnly);
            foreach (var f in txtFiles)
            {
                DebugBox.AppendText("reading snippet filename: " + f + "\n");
                try
                {
                    var snippetFile = ParseTxtSnippetFile(f, matID, fixNs, fetchLayerFromFile);
                    if (snippetFile != null)
                        snippetFolderContents.Snippet_File_List.Add(snippetFile);
                }
                catch (Exception ex)
                {
                    DebugBox.AppendText($"Error parsing txt file '{f}': {ex.Message}\n");
                }
            }

            // handle .pcd files (now handles ascii, binary and binary_compressed)
            string[] pcdFiles = Directory.GetFiles(foldername, "*.pcd", SearchOption.TopDirectoryOnly);
            foreach (var f in pcdFiles)
            {
                DebugBox.AppendText("reading PCD filename: " + f + "\n");
                try
                {
                    var snippetFile = ParsePcdSnippetFile(f, matID, fixNs, fetchLayerFromFile);
                    if (snippetFile != null)
                        snippetFolderContents.Snippet_File_List.Add(snippetFile);
                }
                catch (Exception ex)
                {
                    DebugBox.AppendText($"Error parsing pcd file '{f}': {ex.Message}\n");
                }
            }

            return snippetFolderContents;
        }

        //
        // High level PCD wrapper: reads header, dispatches to ascii/binary handlers.
        // Re-uses your existing ParseAsciiPcdSnippetFile for final conversion.
        //
        Snippet_File ParsePcdSnippetFile(string path, int matID, double[] fixNs, bool fetchLayerFromFile)
        {
            // read all bytes (simpler to split header / data reliably)
            var all = File.ReadAllBytes(path);
            var headerEndIndex = FindHeaderEndIndex(all);
            if (headerEndIndex < 0)
                throw new Exception("PCD header not found or malformed");

            var headerBytes = all.Take(headerEndIndex).ToArray();
            var dataBytes = all.Skip(headerEndIndex).ToArray();
            var headerText = Encoding.ASCII.GetString(headerBytes).Replace("\r", "");

            // parse header lines into a dictionary / known fields
          //var headerLines = headerText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList();
            var headerLines = headerText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList();


            string dataMode = "ascii";
            string[] fields = null;
            int[] sizes = null;
            char[] types = null;
            int[] counts = null;
            int points = -1;

            foreach (var line in headerLines)
            {
                if (line.StartsWith("FIELDS "))
                    fields = line.Substring(7).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                else if (line.StartsWith("SIZE "))
                    sizes = line.Substring(5).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
                else if (line.StartsWith("TYPE "))
                    types = line.Substring(5).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s[0]).ToArray();
                else if (line.StartsWith("COUNT "))
                    counts = line.Substring(6).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
                else if (line.StartsWith("POINTS "))
                    points = int.Parse(line.Substring(7));
                else if (line.StartsWith("DATA "))
                    dataMode = line.Substring(5).Trim().ToLowerInvariant();
            }

            if (fields == null || sizes == null || types == null || counts == null)
                throw new Exception("Incomplete PCD header (FIELDS, SIZE, TYPE and COUNT required)");

            if (points <= 0)
            {
                // fallback: try WIDTH*HEIGHT
                int width = -1, height = 1;
                foreach (var line in headerLines)
                {
                    if (line.StartsWith("WIDTH ")) width = int.Parse(line.Substring(6));
                    if (line.StartsWith("HEIGHT ")) height = int.Parse(line.Substring(7));
                }
                if (width > 0) points = width * Math.Max(1, height);
            }

            // route
            if (dataMode == "ascii")
            {
                // delegate to existing ascii parser which expects a filename
                return (Snippet_File)ParseAsciiPcdSnippetFile(path, matID, fixNs, fetchLayerFromFile);
            }
            else if (dataMode == "binary")
            {
                var temp = ConvertBinaryToTempAsciiPcd(path, headerText, dataBytes, fields, sizes, types, counts, points);
                try
                {
                    //return (Snippet_File)ParseAsciiPcdSnippetFile(temp, matID, fixNs, fetchLayerFromFile);
                    string originalName = Path.GetFileNameWithoutExtension(path);
                    return ParseAsciiPcdSnippetFile(temp, matID, fixNs, fetchLayerFromFile, originalName);
                }
                finally { File.Delete(temp); }
            }
            else if (dataMode == "binary_compressed")
            {
                var uncompressed = UnpackPcdBinaryCompressed(dataBytes, points, sizes, counts);
                var temp = ConvertBinaryToTempAsciiPcd(path, headerText, uncompressed, fields, sizes, types, counts, points);
                try
                {
                    //return (Snippet_File)ParseAsciiPcdSnippetFile(temp, matID, fixNs, fetchLayerFromFile);
                    string originalName = Path.GetFileNameWithoutExtension(path);
                    return ParseAsciiPcdSnippetFile(temp, matID, fixNs, fetchLayerFromFile, originalName);
                }
                finally { File.Delete(temp); }
            }
            else
            {
                throw new Exception("Unsupported DATA mode: " + dataMode);
            }
        }

        int FindHeaderEndIndex(byte[] all)
        {
            // Search for "DATA " anywhere in the file
            var needle = Encoding.ASCII.GetBytes("DATA ");
            int idx = IndexOfSequence(all, needle);
            if (idx < 0)
                return -1;

            // Find the end of the DATA line (handle both \n and \r\n)
            for (int i = idx; i < all.Length; i++)
            {
                if (all[i] == (byte)'\n')
                {
                    // Return position immediately AFTER newline
                    return i + 1;
                }
            }

            // If we never found a newline after DATA, header is malformed
            return -1;
        }


        int IndexOfSequence(byte[] hay, byte[] needle)
        {
            if (needle.Length == 0) return 0;
            for (int i = 0; i <= hay.Length - needle.Length; ++i)
            {
                bool ok = true;
                for (int j = 0; j < needle.Length; ++j)
                {
                    if (hay[i + j] != needle[j]) { ok = false; break; }
                }
                if (ok) return i;
            }
            return -1;
        }

        byte[] UnpackPcdBinaryCompressed(byte[] dataBytes, int pointCount, int[] sizes, int[] counts)
        {
            int compressedSize = BitConverter.ToInt32(dataBytes, 0);
            int uncompressedSize = BitConverter.ToInt32(dataBytes, 4);

            byte[] compressed = new byte[compressedSize];
            Array.Copy(dataBytes, 8, compressed, 0, compressedSize);

            // Step 1: normal LZF decompress (this part WAS correct idea)
            byte[] decompressed = LzfDecompressChunked(compressed, uncompressedSize);

            // Step 2: rebuild interleaved point layout
            int fieldCount = sizes.Length;

            int[] fieldSizes = new int[fieldCount];
            for (int i = 0; i < fieldCount; i++)
                fieldSizes[i] = sizes[i] * counts[i];

            int bytesPerPoint = fieldSizes.Sum();
            byte[] result = new byte[pointCount * bytesPerPoint];

            int srcOffset = 0;

            for (int f = 0; f < fieldCount; f++)
            {
                int fieldBytes = fieldSizes[f] * pointCount;

                for (int p = 0; p < pointCount; p++)
                {
                    int dst = p * bytesPerPoint + fieldSizes.Take(f).Sum();
                    Array.Copy(decompressed, srcOffset + p * fieldSizes[f], result, dst, fieldSizes[f]);
                }

                srcOffset += fieldBytes;
            }

            return result;
        }



        //
        // Convert raw binary point buffer to a temporary ASCII PCD file and return path.
        // headerText is the header read earlier (we will replace the DATA line with "DATA ascii").
        // dataBuffer contains raw point bytes (uncompressed for compressed case).
        //
        string ConvertBinaryToTempAsciiPcd(string originalPath, string headerText, byte[] dataBuffer,
            string[] fields, int[] sizes, char[] types, int[] counts, int points)
        {
            // compute bytes per point
            long bytesPerPoint = 0;
            for (int i = 0; i < sizes.Length; ++i) bytesPerPoint += sizes[i] * counts[i];
            if (bytesPerPoint <= 0) throw new Exception("invalid bytes per point");

            if (points <= 0) points = (int)(dataBuffer.Length / bytesPerPoint);

            var sb = new StringBuilder();

            // write header but with DATA ascii and updated POINTS if necessary
            //var headerLines = headerText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList();
            var headerLines = headerText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList();

            for (int i = 0; i < headerLines.Count; ++i)
            {
                var l = headerLines[i];
                if (l.StartsWith("DATA "))
                {
                    sb.AppendLine("DATA ascii");
                }
                else if (l.StartsWith("POINTS "))
                {
                    sb.AppendLine("POINTS " + points);
                }
                else
                    sb.AppendLine(l);
            }

            // parse the binary data point-by-point and append ASCII lines
            int offset = 0;
            var culture = CultureInfo.InvariantCulture;

            for (int p = 0; p < points; ++p)
            {
                if (offset + bytesPerPoint > dataBuffer.Length)
                    break; // truncated file, stop gracefully

                var values = new List<string>();

                int off = offset;
                for (int f = 0; f < fields.Length; ++f)
                {
                    int fieldCount = counts[f];
                    int fieldSize = sizes[f];
                    char fieldType = types[f];

                    for (int c = 0; c < fieldCount; ++c)
                    {
                        if (off + fieldSize > dataBuffer.Length) { values.Add("0"); off += fieldSize; continue; }

                        // interpret based on size and type
                        if (fieldSize == 4 && (fieldType == 'F' || fieldType == 'f'))
                        {
                            float v = BitConverter.ToSingle(dataBuffer, off);
                            if (!BitConverter.IsLittleEndian)
                            {
                                // if running on big-endian machine, reverse bytes
                                var tmp = new byte[4];
                                Array.Copy(dataBuffer, off, tmp, 0, 4);
                                Array.Reverse(tmp);
                                v = BitConverter.ToSingle(tmp, 0);
                            }
                            values.Add(v.ToString("R", culture));
                        }
                        else if (fieldSize == 8 && (fieldType == 'F' || fieldType == 'f'))
                        {
                            double v = BitConverter.ToDouble(dataBuffer, off);
                            if (!BitConverter.IsLittleEndian)
                            {
                                var tmp = new byte[8];
                                Array.Copy(dataBuffer, off, tmp, 0, 8);
                                Array.Reverse(tmp);
                                v = BitConverter.ToDouble(tmp, 0);
                            }
                            values.Add(v.ToString("R", culture));
                        }
                        else if (fieldSize == 4 && (fieldType == 'I' || fieldType == 'i' || fieldType == 'U' || fieldType == 'u'))
                        {
                            int v = BitConverter.ToInt32(dataBuffer, off);
                            if (!BitConverter.IsLittleEndian)
                            {
                                var tmp = new byte[4];
                                Array.Copy(dataBuffer, off, tmp, 0, 4);
                                Array.Reverse(tmp);
                                v = BitConverter.ToInt32(tmp, 0);
                            }
                            values.Add(v.ToString(culture));
                        }
                        else if (fieldSize == 2)
                        {
                            short v = BitConverter.ToInt16(dataBuffer, off);
                            if (!BitConverter.IsLittleEndian)
                            {
                                var tmp = new byte[2];
                                Array.Copy(dataBuffer, off, tmp, 0, 2);
                                Array.Reverse(tmp);
                                v = BitConverter.ToInt16(tmp, 0);
                            }
                            values.Add(v.ToString(culture));
                        }
                        else if (fieldSize == 1)
                        {
                            byte v = dataBuffer[off];
                            values.Add(v.ToString(culture));
                        }
                        else
                        {
                            // fallback - just try to read 4 bytes as float if possible
                            if (fieldSize >= 4 && off + 4 <= dataBuffer.Length)
                            {
                                float v = BitConverter.ToSingle(dataBuffer, off);
                                values.Add(v.ToString("R", culture));
                            }
                            else
                            {
                                values.Add("0");
                            }
                        }

                        off += fieldSize;
                    }
                }

                sb.AppendLine(string.Join(" ", values));
                offset += (int)bytesPerPoint;
            }

            // write temp file
            var tempPath = Path.ChangeExtension(Path.GetTempFileName(), ".pcd");
            File.WriteAllText(tempPath, sb.ToString(), Encoding.ASCII);
            return tempPath;
        }

        byte[] LzfDecompressChunked(byte[] input, int expectedSize)
        {
            byte[] output = new byte[expectedSize];

            int ip = 0;
            int op = 0;

            while (ip < input.Length)
            {
                int ctrl = input[ip++] & 0xFF;

                if (ctrl < 32)
                {
                    int len = ctrl + 1;
                    Array.Copy(input, ip, output, op, len);
                    ip += len;
                    op += len;
                }
                else
                {
                    int len = ctrl >> 5;
                    int refOffset = op - ((ctrl & 0x1F) << 8) - 1;

                    if (len == 7)
                        len += input[ip++];

                    refOffset -= input[ip++];

                    len += 2;

                    for (int i = 0; i < len; i++)
                        output[op++] = output[refOffset + i];
                }
            }

            return output;
        }


        private Snippet_File ParseTxtSnippetFile(string filepath, int matID, double[] fixNs, bool fetchLayerFromFile)
        {
            // This mostly preserves your original .txt code path which used PCCommonFunctions helpers
            string[] fileLines = File.ReadAllLines(filepath);

            // Parse header: Always process the first line of the file, even if it starts with "//"
            string headerLine = fileLines[0].TrimStart('/').Trim();
            string[] headers = headerLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var columnMap = headers.Select((name, index) => new { name, index }).ToDictionary(x => x.name.ToUpper(), x => x.index);

            // Convert the text file into a string matrix representation for further processing
            string[,] PCDFileStringMatrix = PCCommonFunctions.TextFileToStringMatrix(filepath, ' ', 2);
            float[,] PCDFileFloatMatrix = PCCommonFunctions.StringMatrixToFloatMatrix(PCDFileStringMatrix);

            var snippet_File = new Snippet_File();
            snippet_File.filename = Path.GetFileNameWithoutExtension(filepath);

            for (int row = 0; row < PCDFileFloatMatrix.GetLength(0); row++)
            {
                var snippet_Point = new Snippet_Point();

                snippet_Point.distance = Math.Sqrt(
                    PCDFileFloatMatrix[row, columnMap["X"]] * PCDFileFloatMatrix[row, columnMap["X"]]
                    + PCDFileFloatMatrix[row, columnMap["Y"]] * PCDFileFloatMatrix[row, columnMap["Y"]]
                    + PCDFileFloatMatrix[row, columnMap["Z"]] * PCDFileFloatMatrix[row, columnMap["Z"]]);

                snippet_Point.x = PCDFileFloatMatrix[row, columnMap["X"]];
                snippet_Point.y = PCDFileFloatMatrix[row, columnMap["Y"]];
                snippet_Point.z = PCDFileFloatMatrix[row, columnMap["Z"]];

                if (columnMap.ContainsKey("PEAK"))
                    snippet_Point.PEAK = (int)PCDFileFloatMatrix[row, columnMap["PEAK"]];
                if (columnMap.ContainsKey("AREA"))
                    snippet_Point.AREA = (int)PCDFileFloatMatrix[row, columnMap["AREA"]];
                if (columnMap.ContainsKey("WIDTH"))
                    snippet_Point.WIDTH = (int)PCDFileFloatMatrix[row, columnMap["WIDTH"]];
                if (columnMap.ContainsKey("ECHONUMBER"))
                    snippet_Point.echoNumber = (int)PCDFileFloatMatrix[row, columnMap["ECHONUMBER"]];

                snippet_Point.matID = matID;
                snippet_Point.normal_x = fixNs[0];
                snippet_Point.normal_y = fixNs[1];
                snippet_Point.normal_z = fixNs[2];

                if (fetchLayerFromFile)
                {
                    if (columnMap.ContainsKey("LAYER"))
                        snippet_Point.layer = (int)PCDFileFloatMatrix[row, columnMap["LAYER"]];
                    if (columnMap.ContainsKey("POINT"))
                        snippet_Point.pixelInLayer = (int)PCDFileFloatMatrix[row, columnMap["POINT"]];
                }
                else
                {
                    double r = Math.Sqrt(snippet_Point.x * snippet_Point.x + snippet_Point.y * snippet_Point.y);
                    double angle_v_deg = -57.2958 * Math.Atan2(snippet_Point.z, r);
                    double angle_h_deg = -57.2958 * Math.Atan2(snippet_Point.y, snippet_Point.x);

                    snippet_Point.layer = PCCommonFunctions.IndexFromAngle(angle_v_deg, NUM_LAYERS, POSTPRO_VFOV);
                    snippet_Point.pixelInLayer = PCCommonFunctions.IndexFromAngle(angle_h_deg, NUM_SLOTS, POSTPRO_HFOV);
                }

                snippet_File.Snippet_Point_List.Add(snippet_Point);
            }

            return snippet_File;
        }

        //private Snippet_File ParseAsciiPcdSnippetFile(string filepath, int matID, double[] fixNs, bool fetchLayerFromFile)
        private Snippet_File ParseAsciiPcdSnippetFile(string filepath, int matID, double[] fixNs, bool fetchLayerFromFile, string overrideName = null)
        {
            var allLines = File.ReadAllLines(filepath);
            int linesCount = allLines.Length;

            // find header lines and DATA line
            int dataLineIndex = -1;
            for (int i = 0; i < linesCount; i++)
            {
                var t = allLines[i].Trim();
                if (t.Length == 0) continue;
                if (t.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
                {
                    dataLineIndex = i;
                    break;
                }
            }

            if (dataLineIndex == -1)
                throw new InvalidDataException("PCD file missing DATA line or not a valid PCD.");

            // ensure ascii
            var dataParts = allLines[dataLineIndex].Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (dataParts.Length < 2 || !dataParts[1].Equals("ascii", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("Only ASCII PCD files are supported by this parser. Found: " + allLines[dataLineIndex]);

            // find FIELDS line
            string fieldsLine = allLines.FirstOrDefault(l => l.TrimStart().StartsWith("FIELDS", StringComparison.OrdinalIgnoreCase));
            if (fieldsLine == null)
                throw new InvalidDataException("PCD file missing FIELDS line.");

            string[] fields = fieldsLine.Trim()
                                       .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Skip(1) // skip the "FIELDS" token
                                       .ToArray();

            if (fields.Length < 3)
                throw new InvalidDataException("PCD FIELDS line has fewer than 3 fields (need at least x y z).");

            var columnMap = fields.Select((name, index) => new { name, index }).ToDictionary(x => x.name.ToUpper(), x => x.index);

            // read data lines after DATA line
            var dataRows = new List<float[]>();
            for (int i = dataLineIndex + 1; i < linesCount; i++)
            {
                var line = allLines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("#")) continue; // skip comment lines if any

                // split by whitespace
                string[] tokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0) continue;

                if (tokens.Length != fields.Length)
                {
                    // allow lines that have at least fields.Length (ignore extras) or throw
                    if (tokens.Length < fields.Length)
                        throw new InvalidDataException($"Data row has {tokens.Length} columns but FIELDS specifies {fields.Length} columns (file: {filepath}, line {i + 1}).");
                }

                // parse only the expected number of columns
                var row = new float[fields.Length];
                for (int c = 0; c < fields.Length; c++)
                {
                    // if token missing for this column, set 0
                    if (c >= tokens.Length)
                    {
                        row[c] = 0f;
                        continue;
                    }

                    // parse using invariant culture
                    if (!float.TryParse(tokens[c], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float val))
                    {
                        // If parse fails for integer types, TryParse still fails; try Int32
                        if (int.TryParse(tokens[c], NumberStyles.Integer, CultureInfo.InvariantCulture, out int ival))
                            val = ival;
                        else
                            throw new InvalidDataException($"Unable to parse value '{tokens[c]}' as number in file '{filepath}' on line {i + 1}.");
                    }
                    row[c] = val;
                }

                dataRows.Add(row);
            }

            if (dataRows.Count == 0)
                throw new InvalidDataException("No data rows found in ASCII PCD file.");

            // convert to float[,] matrix
            int rows = dataRows.Count;
            int cols = fields.Length;
            var PCDFileFloatMatrix = new float[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    PCDFileFloatMatrix[r, c] = dataRows[r][c];

            var snippet_File = new Snippet_File();
            snippet_File.filename = (overrideName ?? Path.GetFileNameWithoutExtension(filepath));


            // Now map rows -> Snippet_Point using columnMap (same logic as original)
            for (int row = 0; row < PCDFileFloatMatrix.GetLength(0); row++)
            {
                var snippet_Point = new Snippet_Point();

                // required columns: X Y Z
                if (!columnMap.ContainsKey("X") || !columnMap.ContainsKey("Y") || !columnMap.ContainsKey("Z"))
                    throw new InvalidDataException("PCD FIELDS must contain x y z.");

                snippet_Point.distance = Math.Sqrt(
                    PCDFileFloatMatrix[row, columnMap["X"]] * PCDFileFloatMatrix[row, columnMap["X"]]
                    + PCDFileFloatMatrix[row, columnMap["Y"]] * PCDFileFloatMatrix[row, columnMap["Y"]]
                    + PCDFileFloatMatrix[row, columnMap["Z"]] * PCDFileFloatMatrix[row, columnMap["Z"]]);

                snippet_Point.x = PCDFileFloatMatrix[row, columnMap["X"]];
                snippet_Point.y = PCDFileFloatMatrix[row, columnMap["Y"]];
                snippet_Point.z = PCDFileFloatMatrix[row, columnMap["Z"]];

                if (columnMap.ContainsKey("PEAK"))
                    snippet_Point.PEAK = (int)PCDFileFloatMatrix[row, columnMap["PEAK"]];
                if (columnMap.ContainsKey("AREA"))
                    snippet_Point.AREA = (int)PCDFileFloatMatrix[row, columnMap["AREA"]];
                if (columnMap.ContainsKey("WIDTH"))
                    snippet_Point.WIDTH = (int)PCDFileFloatMatrix[row, columnMap["WIDTH"]];
                // ambientLight or other fields can be read similarly if needed
                if (columnMap.ContainsKey("ECHONUMBER"))
                    snippet_Point.echoNumber = (int)PCDFileFloatMatrix[row, columnMap["ECHONUMBER"]];

                snippet_Point.matID = matID;
                snippet_Point.normal_x = fixNs[0];
                snippet_Point.normal_y = fixNs[1];
                snippet_Point.normal_z = fixNs[2];

                if (fetchLayerFromFile)
                {
                    if (columnMap.ContainsKey("LAYER"))
                        snippet_Point.layer = (int)PCDFileFloatMatrix[row, columnMap["LAYER"]];
                    if (columnMap.ContainsKey("POINT"))
                        snippet_Point.pixelInLayer = (int)PCDFileFloatMatrix[row, columnMap["POINT"]];
                }
                else
                {
                    double r = Math.Sqrt(snippet_Point.x * snippet_Point.x + snippet_Point.y * snippet_Point.y);
                    double angle_v_deg = -57.2958 * Math.Atan2(snippet_Point.z, r);
                    double angle_h_deg = -57.2958 * Math.Atan2(snippet_Point.y, snippet_Point.x);

                    snippet_Point.layer = PCCommonFunctions.IndexFromAngle(angle_v_deg, NUM_LAYERS, POSTPRO_VFOV);
                    snippet_Point.pixelInLayer = PCCommonFunctions.IndexFromAngle(angle_h_deg, NUM_SLOTS, POSTPRO_HFOV);
                }

                snippet_File.Snippet_Point_List.Add(snippet_Point);
            }

            return snippet_File;
        }

        SnippetFolderContents ReadSnippetsFolder(string foldername, int matID1, int matID2, double[] fixNs, bool fetchLayerFromFile)
        {
            SnippetFolderContents snippetFolderContents = new SnippetFolderContents();
            
            string[] fileNameArray = Directory.GetFiles(foldername, "*.txt", SearchOption.TopDirectoryOnly);

            // Perlin noise scale factor - adjust this to change the pattern size
            float noiseScale = 0.1f;

            for (int fileIndex = 0; fileIndex < fileNameArray.GetLength(0); fileIndex++)
            {
                DebugBox.AppendText("reading snippet filename: " + fileNameArray[fileIndex] + "\n");

                // Read all lines from the file into an array for further processing
                string[] fileLines = File.ReadAllLines(fileNameArray[fileIndex]);

                // Parse header
                string headerLine = fileLines[0].TrimStart('/').Trim();
                string[] headers = headerLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                // Map column names to their respective indices
                var columnMap = headers.Select((name, index) => new { name, index }).ToDictionary(x => x.name.ToUpper(), x => x.index);

                // Convert the text file into matrices
                string[,] PCDFileStringMatrix = PCCommonFunctions.TextFileToStringMatrix(fileNameArray[fileIndex], " "[0], 2);
                float[,] PCDFileFloatMatrix = PCCommonFunctions.StringMatrixToFloatMatrix(PCDFileStringMatrix);

                Snippet_File snippet_File = new Snippet_File();
                snippet_File.filename = Path.GetFileNameWithoutExtension(fileNameArray[fileIndex]);

                for (int row = 0; row < PCDFileFloatMatrix.GetLength(0); row++)
                {
                    Snippet_Point snippet_Point = new Snippet_Point();

                    // Get coordinates
                    float x = PCDFileFloatMatrix[row, columnMap["X"]];
                    float y = PCDFileFloatMatrix[row, columnMap["Y"]];
                    float z = PCDFileFloatMatrix[row, columnMap["Z"]];

                    // Initialize material ID with default value
                    snippet_Point.matID = matID1;

                    // Check if point has echoNumber field
                    if (columnMap.ContainsKey("ECHONUMBER"))
                    {
                        int echoNumber = (int)PCDFileFloatMatrix[row, columnMap["ECHONUMBER"]];
                        snippet_Point.echoNumber = echoNumber;

                        // Apply Perlin noise only for actual traffic signs (echoNumber == 1)
                        if (echoNumber == 1)
                        {
                            // Calculate Perlin noise value at this point
                            float noiseValue = PerlinNoise.Noise(x * noiseScale, y * noiseScale, z * noiseScale); //3rd parameter set to fixed value or 0 // Assign material ID based on noise threshold
                            //0.1 = 90% matID1, 10% matID2, 0.3 = 70% matID1, 30% matID2 and so on
                            //snippet_Point.matID = noiseValue > 0.5f ? matID1 : matID2;
                            if (noiseValue > 0.5f)
                            {
                                snippet_Point.matID = matID1;
                            }
                            else
                            {
                                snippet_Point.matID = matID2;
                            }

                        }
                    }

                    // Calculate distance and set coordinates
                    snippet_Point.distance = Math.Sqrt(x * x + y * y + z * z);
                    snippet_Point.x = x;
                    snippet_Point.y = y;
                    snippet_Point.z = z;

                    // Set optional fields if they exist
                    if (columnMap.ContainsKey("PEAK"))
                    {
                        snippet_Point.PEAK = (int)PCDFileFloatMatrix[row, columnMap["PEAK"]];
                    }
                    if (columnMap.ContainsKey("AREA"))
                    {
                        snippet_Point.AREA = (int)PCDFileFloatMatrix[row, columnMap["AREA"]];
                    }
                    if (columnMap.ContainsKey("WIDTH"))
                    {
                        snippet_Point.WIDTH = (int)PCDFileFloatMatrix[row, columnMap["WIDTH"]];
                    }

                    // Set normals
                    snippet_Point.normal_x = fixNs[0];
                    snippet_Point.normal_y = fixNs[1];
                    snippet_Point.normal_z = fixNs[2];

                    // Handle layer information
                    if (fetchLayerFromFile)
                    {
                        if (columnMap.ContainsKey("LAYER"))
                        {
                            snippet_Point.layer = (int)PCDFileFloatMatrix[row, columnMap["LAYER"]];
                        }
                        if (columnMap.ContainsKey("POINT"))
                        {
                            snippet_Point.pixelInLayer = (int)PCDFileFloatMatrix[row, columnMap["POINT"]];
                        }
                    }
                    else
                    {
                        double r = Math.Sqrt(snippet_Point.x * snippet_Point.x + snippet_Point.y * snippet_Point.y);
                        double angle_v_deg = -57.2958 * Math.Atan2(snippet_Point.z, r);
                        double angle_h_deg = -57.2958 * Math.Atan2(snippet_Point.y, snippet_Point.x);

                        snippet_Point.layer = PCCommonFunctions.IndexFromAngle(angle_v_deg, NUM_LAYERS, POSTPRO_VFOV);
                        snippet_Point.pixelInLayer = PCCommonFunctions.IndexFromAngle(angle_h_deg, NUM_SLOTS, POSTPRO_HFOV);

                        //if (sensorVariant == 0)
                        //{
                        //    snippet_Point.layer = PCCommonFunctions.IndexFromAngle(angle_v_deg, NUM_LAYERS_0, POSTPRO_VFOV_0);
                        //    snippet_Point.pixelInLayer = PCCommonFunctions.IndexFromAngle(angle_h_deg, NUM_SLOTS_0, POSTPRO_HFOV);
                        //}
                        //else
                        //{
                        //    snippet_Point.layer = PCCommonFunctions.IndexFromAngle(angle_v_deg, NUM_LAYERS_1, POSTPRO_VFOV_1);
                        //    snippet_Point.pixelInLayer = PCCommonFunctions.IndexFromAngle(angle_h_deg, NUM_SLOTS_1, POSTPRO_HFOV);
                        //}
                    }

                    // Add the point to the file's list
                    snippet_File.Snippet_Point_List.Add(snippet_Point);
                }

                snippetFolderContents.Snippet_File_List.Add(snippet_File);
            }

            return snippetFolderContents;
        }

        public double ComputeHausdorffDistance(Series seriesA, Series seriesB)
        {
            double hAB = 0.0; // Max of min distances from A to B
            double hBA = 0.0; // Max of min distances from B to A

            // For each point in seriesA
            foreach (DataPoint p in seriesA.Points)
            {
                double minDistance = double.PositiveInfinity;
                double px = p.XValue;
                double py = p.YValues[0];

                // For each point in seriesB
                foreach (DataPoint q in seriesB.Points)
                {
                    double qx = q.XValue;
                    double qy = q.YValues[0];
                    double dx = px - qx;
                    double dy = py - qy;
                    double distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }

                if (minDistance > hAB)
                {
                    hAB = minDistance;
                }
            }

            // For each point in seriesB
            foreach (DataPoint q in seriesB.Points)
            {
                double minDistance = double.PositiveInfinity;
                double qx = q.XValue;
                double qy = q.YValues[0];

                // For each point in seriesA
                foreach (DataPoint p in seriesA.Points)
                {
                    double px = p.XValue;
                    double py = p.YValues[0];
                    double dx = qx - px;
                    double dy = qy - py;
                    double distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }

                if (minDistance > hBA)
                {
                    hBA = minDistance;
                }
            }

            // Return the maximum of hAB and hBA
            return Math.Max(hAB, hBA);
        }

        // Perlin noise implementation class
        public static class PerlinNoise
        {
            private static readonly int[] permutation =
            {
                151,160,137,91,90,15,131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,
                8,99,37,240,21,10,23,190,6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,
                35,11,32,57,177,33,88,237,149,56,87,174,20,125,136,171,168,68,175,74,165,71,
                134,139,48,27,166,77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,
                55,46,245,40,244,102,143,54,65,25,63,161,1,216,80,73,209,76,132,187,208,89,
                18,169,200,196,135,130,116,188,159,86,164,100,109,198,173,186,3,64,52,217,226,
                250,124,123,5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,
                189,28,42,223,183,170,213,119,248,152,2,44,154,163,70,221,153,101,155,167,43,
                172,9,129,22,39,253,19,98,108,110,79,113,224,232,178,185,112,104,218,246,97,
                228,251,34,242,193,238,210,144,12,191,179,162,241,81,51,145,235,249,14,239,
                107,49,192,214,31,181,199,106,157,184,84,204,176,115,121,50,45,127,4,150,254,
                138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
            };

            private static readonly int[] p = new int[512];

            static PerlinNoise()
            {
                for (int i = 0; i < 256; i++)
                {
                    p[256 + i] = p[i] = permutation[i];
                }
            }

            public static float Noise(float x, float y, float z)
            {
                // Find unit cube that contains point
                int X = (int)Math.Floor(x) & 255;
                int Y = (int)Math.Floor(y) & 255;
                int Z = (int)Math.Floor(z) & 255;

                // Find relative x,y,z of point in cube
                x -= (float)Math.Floor(x);
                y -= (float)Math.Floor(y);
                z -= (float)Math.Floor(z);

                // Compute fade curves for each of x,y,z
                float u = Fade(x);
                float v = Fade(y);
                float w = Fade(z);

                // Hash coordinates of the 8 cube corners
                int A = p[X] + Y;
                int AA = p[A] + Z;
                int AB = p[A + 1] + Z;
                int B = p[X + 1] + Y;
                int BA = p[B] + Z;
                int BB = p[B + 1] + Z;

                // Add blended results from 8 corners of cube
                float res = Lerp(w, Lerp(v, Lerp(u, Grad(p[AA], x, y, z),
                                    Grad(p[BA], x - 1, y, z)),
                            Lerp(u, Grad(p[AB], x, y - 1, z),
                                Grad(p[BB], x - 1, y - 1, z))),
                        Lerp(v, Lerp(u, Grad(p[AA + 1], x, y, z - 1),
                                Grad(p[BA + 1], x - 1, y, z - 1)),
                            Lerp(u, Grad(p[AB + 1], x, y - 1, z - 1),
                                Grad(p[BB + 1], x - 1, y - 1, z - 1))));
                return (res + 1.0f) / 2.0f; // Normalize to [0,1]
            }

            private static float Fade(float t)
            {
                return t * t * t * (t * (t * 6 - 15) + 10);
            }

            private static float Lerp(float t, float a, float b)
            {
                return a + t * (b - a);
            }

            private static float Grad(int hash, float x, float y, float z)
            {
                int h = hash & 15;
                float u = h < 8 ? x : y;
                float v = h < 4 ? y : h == 12 || h == 14 ? x : z;
                return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
            }
        }
    }
}