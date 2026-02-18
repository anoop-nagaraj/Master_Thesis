using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;
using static scalagen2fssp_hifi_calibrator.MainForm;

namespace scalagen2fssp_hifi_calibrator
{
    public partial class MainForm : Form
    {
        class ReportSection
        {
            public string Title { get; set; }                  // e.g., "Noise Parameter Analysis"
            public List<string> Panels { get; set; }           // Panels saves file names
            public List<(double bias, double normBias, double cavm)> AreaMetrics { get; set; }
            public List<(double bias, double normBias, double cavm)> DistanceMetrics { get; set; }
            public Dictionary<string, List<string>> PanelImages { get; set; } // Key: panel name, Value: list of image paths for that panel

            public ReportSection()
            {
                Panels = new List<string>();
                AreaMetrics = new List<(double, double, double)>();
                DistanceMetrics = new List<(double, double, double)>();
                PanelImages = new Dictionary<string, List<string>>();
            }
        }

        private void button_generateReport_Click(object sender, EventArgs e)
        {
            SetConstants();

            var sections = new List<ReportSection>();

            // Noise Characteristics
            if (checkBox_Statistics.Checked)
            {
                checkBox_Statistics_writeOutputToFolder.Checked = true; // Create Merged_SIM.pcd file of all the panels
                //filesMerged.Value = 1;  // To be deleted
                //comboBox_sensorVariant.SelectedIndex = 1;   // To be deleted
                button_Statistics_run_Click(sender, EventArgs.Empty); // Execute Noise (panel statistics) simulation

                var noiseSection = GenerateNoiseReportSection();
                sections.Add(noiseSection);
            }

            //Blooming Characteristics
            if (checkBox_BloomingVer.Checked)
            {
                //comboBox_sensorVariant.SelectedIndex = 0; // To be deleted

                button_Panel_BloomingCalibration(sender, EventArgs.Empty); // workaround: results generate only if blooming panel is loaded [Do not delete]
                ShowPanel(panel_Report); // Reload reports panel after processing blooming results

                button_RunModel_BloomingCalibrationV_RunCalibration(sender, EventArgs.Empty); // Execute vertical blooming calibration simulation
                UpdateCharts(2, "Vertical");
                UpdateCharts(3, "Vertical");

                var bloomingSection = GenerateBloomingReportSection();
                sections.Add(bloomingSection);
            }

            // Panel Calibration Surfaces --> Asphalt simulation
            if (checkBox_CalibSurfaces.Checked)
            {
                int sensorVariant = comboBox_sensorVariant.SelectedIndex; // Smart or Slim or Satellite
                checkBox_snippetsFolder_Surf_1.Checked = false; // Disable 3-percent simuation section as it is Enabled by default
                checkBox_snippets_writeModelOutputIntoFolder_Surf.Checked = true; // Save Simulated pcd of Asphalt

                string matID_Surf_Txt = sensorVariant == 0 ? textBox_snippetsMatID_Surf_2_Smart.Text : sensorVariant == 1 ? textBox_snippetsMatID_Surf_2_Slim.Text : 
                                                             textBox_snippetsMatID_Surf_2_Satellite.Text; // Select matID based on variant
                int matID = int.Parse(matID_Surf_Txt);
                //double[] fixNs = new double[3] { 0, 0, 1 };
                string tiltAngle_Text = textBox_tiltAngle_Surf_2_Smart.Text;
                if (sensorVariant == 0) tiltAngle_Text = textBox_tiltAngle_Surf_2_Smart.Text;
                if (sensorVariant == 1) tiltAngle_Text = textBox_tiltAngle_Surf_2_Slim.Text;
                if (sensorVariant == 2) tiltAngle_Text = textBox_tiltAngle_Surf_2_Satellite.Text;
                double tiltAngle_rad = Math.PI / 180.0 * double.Parse(tiltAngle_Text, NumberStyles.Float, CultureInfo.InvariantCulture);
                double[] fixNs = new double[3] { -Math.Tan(tiltAngle_rad), 0, Math.Cos(tiltAngle_rad) };

                string slantRange_Text = sensorVariant == 0 ? textBox_slantRange_Surf_2_Smart.Text : sensorVariant == 1 ? textBox_slantRange_Surf_2_Slim.Text :
                                                              textBox_slantRange_Surf_2_Satellite.Text; // Select slantRange based on variant

                double fixedSlantRange = double.Parse(slantRange_Text, NumberStyles.Float, CultureInfo.InvariantCulture);

                bool fixSlantRange = sensorVariant == 0 ? checkBox_fixSlantrange_Surf_2_Smart.Checked : sensorVariant == 1 ? checkBox_fixSlantrange_Surf_2_Slim.Checked :
                                                          checkBox_fixSlantrange_Surf_2_Satellite.Checked;

                string foldername = sensorVariant == 0 ? textBox_snippetsFolder_Surf_2_Smart.Text : sensorVariant == 1 ? textBox_snippetsFolder_Surf_2_Slim.Text :
                                                         textBox_snippetsFolder_Surf_2_Satellite.Text; // Select input file location based on variant

                SnippetFolderContents snippetFolderContents = ReadSnippetsFolder(foldername, matID, fixNs, true); //Layer and point data is read directly from *.txt file

                /*
                 * pointsPerLayerSim accumulates no.of points present in each layer and returns as a list from simulated pcd
                 * last parameter (fetchLayerFromFile) = true => Layer and point data is read directly from *.txt file
                 */
                var (allEchoSims_dist, allEchoSims_data, pointsPerLayerSim) = ProcessEchoSimulation(snippetFolderContents.Snippet_File_List[0], fixNs, fixedSlantRange, fixSlantRange, false);

                var asphaltSection = GenerateCalibrationSurfacesReportSection(pointsPerLayerSim);
                sections.Add(asphaltSection);
            }

            // Generate combined HTML report from all selected sections
            GenerateHtmlReport(sections);
        }

        private ReportSection GenerateCalibrationSurfacesReportSection(int[] pointsPerLayerSim)
        {
            var section = new ReportSection();

            // Fetch *.txt file path
            var folderPathReal = comboBox_sensorVariant.SelectedIndex == 0 ? textBox_snippetsFolder_Surf_2_Smart.Text :
                                 comboBox_sensorVariant.SelectedIndex == 1 ? textBox_snippetsFolder_Surf_2_Slim.Text :
                                 textBox_snippetsFolder_Surf_2_Satellite.Text;


            // save all *.txt file names
            string[] realFile = Directory.GetFiles(folderPathReal, "*.txt", SearchOption.TopDirectoryOnly);

            // Get layer and point data and max/min of point
            var realLayerPointData = GetRealLayerPointData(realFile);

            var saveImage = @"C:\Projects\Scala3_calibrator_DATA\Images";

            section = PlotLayerProbabilities(realLayerPointData.Data, realLayerPointData.Min, realLayerPointData.Max, pointsPerLayerSim, saveImage);

            return section;
        }

        /*
         * This function processes the *.txt file and returns layer and point data from file
         * It also returns the maximum and minimum point value in order to calculate the denominator for probability
         */
        private (List<(double Layer, double Point)> Data, double Min, double Max) GetRealLayerPointData(string[] filePaths)
        {
            var result = new List<(double Layer, double Point)>();

            foreach (var filePath in filePaths)
            {
                int layerIndex = -1;
                int pointIndex = -1;

                foreach (var line in File.ReadLines(filePath))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Read header and detect indices
                    if (line.StartsWith("//"))
                    {
                        var headerParts = line.Substring(2).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        layerIndex = Array.IndexOf(headerParts, "Layer");
                        pointIndex = Array.IndexOf(headerParts, "Point");
                        continue;
                    }

                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    // Skip if line does not have enough columns
                    if (parts.Length <= Math.Max(layerIndex, pointIndex))
                        continue;

                    if (layerIndex < 0 || pointIndex < 0)
                        continue;

                    if (double.TryParse(parts[layerIndex], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double layer) &&
                        double.TryParse(parts[pointIndex], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double point))
                    {
                        result.Add((layer, point));
                    }
                }
            }

            var min = result.Count > 0 ? result.Min(r => r.Point) : double.NaN;
            var max = result.Count > 0 ? result.Max(r => r.Point) : double.NaN;

            return (result, min, max);
        }


        private ReportSection PlotLayerProbabilities(List<(double Layer, double Point)> layerPointData, double minPoint, double maxPoint, int[] pointsPerLayerSim, string savePath)
        {
            var section = new ReportSection();
            section.Title = "Asphalt Parameter Analysis";

            string inputPath = comboBox_sensorVariant.SelectedIndex == 0 ? @"C:\Projects\Scala3_calibrator_DATA\CalibrationSurfaces\SMART\Asphalt" :
                               comboBox_sensorVariant.SelectedIndex == 1 ? @"C:\Projects\Scala3_calibrator_DATA\CalibrationSurfaces\SLIM\Asphalt" :
                                                                           @"C:\Projects\Scala3_calibrator_DATA\CalibrationSurfaces\SATELLITE\Asphalt_0kLux";

            // get all *.txt files in the folder
            var files = Directory.GetFiles(inputPath, "*.txt");

            // this functionality is implemented to support only 1 data at a time
            string firstFile = files.FirstOrDefault();

            string inputFile = Path.GetFileNameWithoutExtension(firstFile);
            section.Panels.Add(inputFile);

            //int noOfLayers = comboBox_sensorVariant.SelectedIndex == 0 ? NUM_LAYERS_0 : NUM_LAYERS_1;
            int noOfLayers = NUM_LAYERS;
            int sensorVariant = comboBox_sensorVariant.SelectedIndex; 

            // To calculate probability the denominator shall be the maximum no.of points present in a layer of a REAL file upon comparison with all the layers
            double denominator = (maxPoint - minPoint + 1);

            // Count no.of points present in each layer of REAL file (duplicate points are discarded) 
            var layerPointReal = layerPointData
                .GroupBy(x => (int)x.Layer)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Point).Distinct().Count()
                );

            // Count no.of points present in each layer of SIM file (duplicate points are discarded)
            var layerPointSim = pointsPerLayerSim
                .Select((value, index) => new { index, value })
                .Where(x => x.value != 0)
                .ToDictionary(
                    x => sensorVariant == 0 ? (noOfLayers - x.index) : x.index,
                    x => x.value
                );

            /* 
			 * Start and End values is fetched from the GUI 
			 * This limits the region of interest to the step up section of the graph		
			*/
            int start = sensorVariant == 0 ? (int)numeric_smart_min.Value : sensorVariant == 1 ? (int)numeric_slim_min.Value : (int)numeric_satellite_min.Value;

            int end = sensorVariant == 0 ? (int)numeric_smart_max.Value : sensorVariant == 1 ? (int)numeric_slim_max.Value : (int)numeric_satellite_max.Value;

            // No.of points present in each layer in the segmented section of REAL file
            var layerPointReal_filtered = layerPointReal
                .Where(layer => layer.Key >= start && layer.Key <= end)
                .ToDictionary(layer => layer.Key, layer => layer.Value);

			// No.of points present in each layer in the segmented section of SIM file
            var layerPointSim_filtered = layerPointSim
                .Where(kv => kv.Key >= start && kv.Key <= end)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            layerPointReal = layerPointReal_filtered;
            layerPointSim = layerPointSim_filtered;

            // Chart setup for the report
            var chart = new Chart();
            chart.Width = 1200;
            chart.Height = 600;

            var chartArea = new ChartArea();
            chartArea.AxisX.Title = "Layer";
            chartArea.AxisY.Title = "Detection Probability";
            chartArea.AxisX.Minimum = -10;
            chartArea.AxisX.Maximum = 380;
            chartArea.AxisX.Interval = 10;
            chartArea.AxisY.Minimum = 0;
            chartArea.AxisY.Maximum = 1.2;
            chartArea.AxisY.Interval = 0.2;

            chart.ChartAreas.Add(chartArea);

            // Real data series (red)
            var seriesReal = new Series
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 2,
                Color = Color.Red,
                Name = "Real Data"
            };

			//For each real layer calculate probability and add it to seriesReal in order to plot the chart
            for (int layer = 0; layer <= noOfLayers - 1; layer++)
            {
                int realPointCount = layerPointReal.ContainsKey(layer) ? layerPointReal[layer] : 0;
                double probability = (double)realPointCount / denominator;
                seriesReal.Points.AddXY(layer, probability); 
            }


            // Sim data series (blue)
            var seriesSim = new Series
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 2,
                Color = Color.Blue,
                Name = "Sim Data"
            };

			//For each simulated layer calculate probability and add it to seriesSim in order to plot the chart
            for (int layer = 0; layer <= noOfLayers - 1; layer++)
            {
                int simPointCount = layerPointSim.ContainsKey(layer) ? layerPointSim[layer] : 0;
                double probability = (double)simPointCount / denominator;
                seriesSim.Points.AddXY(layer, probability);
            }

            /* 
			 * No ECDF, only KPi metrics
			 * Since probability is already calculated, calculating ECDF doesnt make any sense
			 * With this probability data KPI metrics such as Bias, NormalizedBias and CAVM are calculated
			 */
            var layers = seriesReal.Points.Cast<DataPoint>().Select(p => p.XValue).ToList();
            var realProbabilities = seriesReal.Points.Cast<DataPoint>().Select(p => p.YValues[0]).ToList();
            var simProbabilities = seriesSim.Points.Cast<DataPoint>().Select(p => p.YValues[0]).ToList();

            // biasMetrics_Area consists of Bias, NormalizedBias and CAVM
			var biasMetrics_Area = ComputeOverallMetrics(realProbabilities, simProbabilities, layers);
            section.AreaMetrics.Add(biasMetrics_Area);

            // Determine dynamic X-axis range based on data
            var allPoints = seriesReal.Points.Concat(seriesSim.Points)
                            .Where(p => p.YValues[0] > 0)
                            .ToList();

            if (allPoints.Any())
            {
                double minX = allPoints.Min(p => p.XValue);
                double maxX = allPoints.Max(p => p.XValue);

                // Add some buffer, e.g., 10 units on each side
                chartArea.AxisX.Minimum = minX - 3;
                chartArea.AxisX.Maximum = maxX + 3;

                // Optional: adjust interval to avoid too many tick marks
                chartArea.AxisX.Interval = Math.Max(1, (maxX - minX) / 10);
            }
            else
            {
                // fallback if no non-zero values
                chartArea.AxisX.Minimum = start - 10;
                chartArea.AxisX.Maximum = end + 10;
            }


            chart.Series.Add(seriesReal);
            chart.Series.Add(seriesSim);

            // Add legend so colors are clear
            chart.Legends.Add(new Legend());

            // Save chart
            Directory.CreateDirectory(savePath);
			
			/* 
			 * fileName should contain keyword "area" eventhough it doesnt correspond to area 
			 * because the filenames are filtered as such while generating the report
			*/
            string fileName = Path.Combine(savePath, "AsphaltDetectionProbability_area.png");
            chart.SaveImage(fileName, ChartImageFormat.Png);

            section.PanelImages[inputFile] = new List<string> { fileName };

            return section;
        }

        public static (double bias, double normalizedBias, double cavm) ComputeOverallMetrics(List<double> realProbabilities, List<double> simProbabilities, List<double> layers)
        {
            double biasArea = 0.0, realArea = 0.0, cavmArea = 0.0;

            for (int i = 0; i < layers.Count - 1; i++)
            {
                double dx = layers[i + 1] - layers[i];

                double realLeft = realProbabilities[i];
                double realRight = realProbabilities[i + 1];

                double simLeft = simProbabilities[i];
                double simRight = simProbabilities[i + 1];

                double diffLeft = simLeft - realLeft;
                double diffRight = simRight - realRight;

                // bias (signed difference)
                biasArea += 0.5 * (diffLeft + diffRight) * dx;

                // absolute difference for CAVM
                cavmArea += 0.5 * (Math.Abs(diffLeft) + Math.Abs(diffRight)) * dx;

                // reference area (real)
                realArea += 0.5 * (realLeft + realRight) * dx;
            }

            double normalizedBias = realArea != 0 ? biasArea / realArea : 0.0;
            double cavm = realArea != 0 ? cavmArea / realArea : 0.0;

            return (biasArea, normalizedBias, cavm);
        }

        private ReportSection GenerateNoiseReportSection()
        {
            // Create new section object
            var section = new ReportSection();
            section.Title = "Noise Parameter Analysis";

            // Define input/output paths
            //int sensorVariant = comboBox_sensorVariant.SelectedIndex;
            //var folderPathReal = @"C:\Projects\Scala3_calibrator_DATA\StatisticsDataForComparison\SLIM\REAL";

            int sensorVariant = comboBox_sensorVariant.SelectedIndex;
            string sensorFolder;
            switch (sensorVariant)
            {
                case 0:
                    sensorFolder = "SMART";
                    break;
                case 1:
                    sensorFolder = "SLIM";
                    break;
                case 2:
                    sensorFolder = "SATELLITE";
                    break;
                default:
                    throw new InvalidOperationException("Unknown sensor variant");
            }

            string folderPathReal = $@"C:\Projects\Scala3_calibrator_DATA\StatisticsDataForComparison\{sensorFolder}\REAL";
            var folderPathSim = @"C:\Projects\Scala3_calibrator_DATA\StatisticsDataForComparison\Output\Merged_SIM.pcd";
            var outputFolder = @"C:\Projects\Scala3_calibrator_DATA\Images";

            // Load real data
            // Returns tuples of (fileName, areaList)
            var RealAreaData = LoadRealAreaValuesFromFiles(folderPathReal, "noise"); // (FileName , Area) of all the files as a list
            var RealAreaLists = RealAreaData.Select(t => t.areaList).ToList();       // Only area of all the files as a list
            var RealDistanceData = LoadRealDistanceValuesFromFiles(folderPathReal);  // Calculates Euclidian distance of each point and saved as a list
            var fileNames = RealAreaData.Select(t => t.fileName).ToList();			 // List of all File names

            // Load simulated data from merged PCD
            var SimGroupedAreas = LoadAreaValuesFromMergedPCD(folderPathSim);        // Simulated Areas categorised based on distance and seperated into a list
            var SimGroupedDistance = LoadDistanceValuesFromMergedPCD(folderPathSim); // Euclidian distance of each point

            // Compute ECDFs for area and distance
            var realECDFs = ComputeECDFs(RealAreaLists);
            var simECDFs = ComputeECDFs(SimGroupedAreas.Select(group => group.Select(x => (double)x)));

            var realDistanceECDFs = ComputeECDFs(RealDistanceData);
            var simDistanceECDFs = ComputeECDFs(SimGroupedDistance);

            // Generate ECDF comparison charts and save images
            SaveECDFCharts(realECDFs, simECDFs, fileNames, outputFolder, "AREA", "Noise_area_");
            SaveECDFCharts(realDistanceECDFs, simDistanceECDFs, fileNames, outputFolder, "DISTANCE", "Noise_distance_");

            // Compute bias metrics for area and distance
            var biasMetrics_Area = ComputeBiasMetricsPerGroup( realECDFs, simECDFs, RealAreaLists, SimGroupedAreas.Select(g => g.Select(x => (double)x).ToList()).ToList() );

            var biasMetrics_Distance = ComputeBiasMetricsPerGroup( realDistanceECDFs, simDistanceECDFs, RealDistanceData, SimGroupedDistance );

            // Generate global detection probability chart
            GenerateDetectionProbabilityCharts(Path.Combine(outputFolder, "Noise_detection_prob_chart.png"));

            // Fill section object with panel names and metrics
            section.Panels = fileNames;
            section.AreaMetrics = biasMetrics_Area;
            section.DistanceMetrics = biasMetrics_Distance;

            // Add image paths for each panel (area & distance charts + ECDF charts)
            for (int i = 0; i < fileNames.Count; i++)
            {
                var panel = fileNames[i];
                section.PanelImages[panel] = new List<string>
                {
                    Path.Combine(outputFolder, $"Noise_AreaChart_{panel}.png"),
                    Path.Combine(outputFolder, $"Noise_area_ecdfChart_{panel}.png"),
                    Path.Combine(outputFolder, $"Noise_DistanceChart_{panel}.png"),
                    Path.Combine(outputFolder, $"Noise_distance_ecdfChart_{panel}.png"),
                };
            }

            // Add global detection probability chart
            section.PanelImages["DetectionProbability"] = new List<string>
            {
                Path.Combine(outputFolder, "Noise_detection_prob_chart.png")
            };

            return section;
        }

        private ReportSection GenerateBloomingReportSection()
        {
            // Create new section object
            var section = new ReportSection();
            section.Title = "Blooming Parameter Analysis";

            // Determine sensor type (SMART or SLIM) from the UI selection to construct correct file paths.
            int sensorVariant = comboBox_sensorVariant.SelectedIndex;
            string sensorType = sensorVariant == 0 ? "SMART" : sensorVariant == 1 ? "SLIM" : "SATELLITE";

            // Define the input path for real data files and the output path for generated images.
            string folderPathReal = Path.Combine(@"C:\Projects\Scala3_calibrator_DATA\Blooming", sensorType, "REAL");
            var outputFolder = @"C:\Projects\Scala3_calibrator_DATA\Images";

            // Load real area data for the "VerBlooming" attribute
            var RealAreaData = LoadRealAreaValuesFromFiles(folderPathReal, "VerBlooming");
            // Extract the file names to use as identifiers for panels in the report.
            var fileNames = RealAreaData.Select(t => t.fileName).ToList();

            // Compute bias metrics (bias & normalized bias) by comparing the raw values of
            // real and simulated data for the right part of the distribution.
            // Note: This uses a direct comparison (`_Raw`) instead of an ECDF-based method.
            var biasMetrics_Area = ComputeBiasMetricsPerGroup_Raw(rightPartReal_Area_All, rightPartSim_Area_All);

            // Populate the report section with the list of panel names and the calculated area metrics.
            section.Panels = fileNames;
            section.AreaMetrics = biasMetrics_Area;

            // Loop through each panel (file) to find and associate the corresponding generated chart images.
            for (int i = 0; i < fileNames.Count; i++)
            {
                var panel = fileNames[i];
                var images = new List<string>();

                // Check for and add the path to the vertical blooming histogram image if it was generated.
                var verticalImage = Path.Combine(outputFolder, $"Blooming_area_vertical_{i + 1}.png");
                if (File.Exists(verticalImage))
                    images.Add(verticalImage);

                // Check for and add the path to the chart showing the focused (right part) distribution area.
                var focusedArea = Path.Combine(outputFolder, $"AreaRightPart_Vertical_Dist{i+1}.png");
                if (File.Exists(focusedArea))
                    images.Add(focusedArea);

                // Assign the list of found images to the corresponding panel in the report section.
                section.PanelImages[panel] = images;
            }

            return section;
        }

        private List<(double Bias, double NormalizedBias, double CAVM)> ComputeBiasMetricsPerGroup_Raw( List<List<double>> realValues_All, List<List<double>> simValues_All)
        {
            var results = new List<(double, double, double)>();

            int groupCount = Math.Min(realValues_All.Count, simValues_All.Count);
            for (int i = 0; i < groupCount; i++)
            {
                var realValues = realValues_All[i];
                var simValues = simValues_All[i];

                int n = Math.Min(realValues.Count, simValues.Count);
                if (n == 0)
                {
                    results.Add((double.NaN, double.NaN, double.NaN));
                    continue;
                }

                // Basic mean-based bias
                double meanReal = realValues.Average();
                double meanSim = simValues.Average();
                double bias = meanSim - meanReal;

                // Normalized bias relative to real value range
                double range = realValues.Max() - realValues.Min();
                double normalizedBias = range != 0 ? bias / range : 0;

                // CAVM cannot be calculated without ECDF, so skipped
                double cavm = double.NaN;

                results.Add((bias, normalizedBias, cavm));
            }

            return results;
        }

        /*
         * Loads "real" area values from all TXT files in the given folder.
         * Returns: A list of tuples, where each tuple contains:
         * fileName: name of the TXT file (without extension)
         * areaList: list of parsed area values for that file
         */
        private List<(string fileName, List<double> areaList)> LoadRealAreaValuesFromFiles(string folderPath, string attribute)
        {
            // Get all .txt files in the provided folder
            var filePaths = Directory.GetFiles(folderPath, "*.txt");
            var result = new List<(string fileName, List<double> areaList)>();

            // Decide which column index to read, based on the attribute
            int areaIndex;
            if (attribute == "noise")
            {
                areaIndex = 3;
            }
            else if (attribute == "VerBlooming")
            {
                areaIndex = 7;
            }
            else
            {
                throw new ArgumentException($"Unsupported attribute: {attribute}");
            }

            foreach (var filePath in filePaths)
            {
                var areaValues = new List<double>();
                var lines = File.ReadAllLines(filePath);

                foreach (var line in lines)
                {
                    // Skip empty lines, comments (//), or header lines (starting with a letter)
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//") || char.IsLetter(line[0]))
                        continue;

                    // Split row into parts using whitespace as delimiter
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    // Skip if the expected column does not exist
                    if (parts.Length <= areaIndex)
                        continue;

                    // Try parsing the value in the chosen column
                    if (double.TryParse(parts[areaIndex], out double area))
                    {
                        areaValues.Add(area);
                    }
                }

                // Store filename (without extension) together with collected values
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                result.Add((fileName, areaValues));
            }

            return result;
        }

        /*
         * Loads distance values from all TXT files in the given folder.
         * The function computes the Euclidean distance for each row: distance = sqrt(x² + y² + z²)
         * Returns: A list of lists, where each inner list contains all computed distances from one file in the folder.
        */
        private List<List<double>> LoadRealDistanceValuesFromFiles(string folderPath)
        {
            var allDistancesPerFile = new List<List<double>>();
            var filePaths = Directory.GetFiles(folderPath, "*.txt");

            foreach (var filePath in filePaths)
            {
                var distances = new List<double>();
                var lines = File.ReadAllLines(filePath);

                foreach (var line in lines)
                {
                    // Skip empty lines, comments (//), or headers starting with a letter
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//") || char.IsLetter(line[0]))
                        continue;

                    // Split line into columns
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    // Must have at least X, Y, Z columns
                    if (parts.Length < 3)
                        continue;

                    // Try parsing first three columns as doubles (X, Y, Z)
                    if (double.TryParse(parts[0], out double x) &&
                        double.TryParse(parts[1], out double y) &&
                        double.TryParse(parts[2], out double z))
                    {
                        // Compute Euclidean distance from origin
                        double distance = Math.Sqrt(x * x + y * y + z * z);
                        distances.Add(distance);
                    }
                }

                // Add all distances from this file to the result
                allDistancesPerFile.Add(distances);
            }

            return allDistancesPerFile;
        }

        /*  
           Loads area values from a merged PCD file and groups them by proximity of the X coordinate.

           Assumptions about data layout:
           - Column 0 → X coordinate
           - Column 4 → Area value (integer)

           Logic:
           - Reads each row after "DATA ascii".
           - For each row, extracts X and Area.
           - Groups Area values into bins based on X proximity:
               * If X is within ±2.0 units of an existing bin center, add Area to that bin.
               * Otherwise, create a new bin centered on this X.

           Returns:
           - A list of groups, where each group is a list of Area values
             corresponding to one X-region/bin.
        */

        private List<List<int>> LoadAreaValuesFromMergedPCD(string filePath)
        {
            var areaGroups = new List<List<int>>(); // Groups of area values clustered by X
            var xCenters = new List<double>();      // Representative X values for each group/bin

            bool dataSectionStarted = false;

            foreach (var line in File.ReadLines(filePath))
            {
                // Skip header until we reach the "DATA ascii" marker
                if (!dataSectionStarted)
                {
                    if (line.Trim().Equals("DATA ascii", StringComparison.OrdinalIgnoreCase))
                        dataSectionStarted = true;

                    continue;
                }

                // Split into columns
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5) // Need at least X and Area column
                    continue;

                // Parse X and Area
                if (!double.TryParse(parts[0], out double x)) continue;
                if (!int.TryParse(parts[4], out int area)) continue;

                // Try to find an existing bin (based on X proximity threshold)
                bool added = false;
                for (int i = 0; i < xCenters.Count; i++)
                {
                    if (Math.Abs(x - xCenters[i]) < 2.0)
                    {
                        areaGroups[i].Add(area);
                        added = true;
                        break;
                    }
                }

                // If no bin matched, create a new one
                if (!added)
                {
                    xCenters.Add(x);
                    areaGroups.Add(new List<int> { area });
                }
            }

            return areaGroups;
        }

        /*  
         * Reads a merged PCD file and groups distances of points from the origin (0,0,0).  
         * Groups are split when there is a spatial discontinuity between consecutive points.  
        */
        private List<List<double>> LoadDistanceValuesFromMergedPCD(string filePath)
        {
            var groupedDistances = new List<List<double>>(); // Final collection of groups
            var currentGroup = new List<double>();           // Temporary group of distances

            bool dataSectionStarted = false; // Tracks when "DATA ascii" section starts
            double? prevX = null;            // Stores previous point's X coordinate
            double? prevY = null;            // Stores previous point's Y coordinate
            double? prevZ = null;            // Stores previous point's Z coordinate

            foreach (var line in File.ReadLines(filePath))
            {
                // Skip lines until reaching the actual point data
                if (!dataSectionStarted)
                {
                    if (line.Trim().Equals("DATA ascii", StringComparison.OrdinalIgnoreCase))
                        dataSectionStarted = true;
                    continue;
                }

                // Split line into parts (x, y, z, ...)
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3) continue; // Need at least x, y, z

                // Parse coordinates
                if (!double.TryParse(parts[0], out double x)) continue;
                if (!double.TryParse(parts[1], out double y)) continue;
                if (!double.TryParse(parts[2], out double z)) continue;

                // Compute distance from the origin
                double distanceFromOrigin = Math.Sqrt(x * x + y * y + z * z);

                if (prevX.HasValue) // Not the first point
                {
                    // Compute distance between this point and the previous one
                    double dx = x - prevX.Value;
                    double dy = y - prevY.Value;
                    double dz = z - prevZ.Value;

                    double spatialJump = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                    // If the gap between consecutive points is too large, start a new group
                    if (spatialJump > 5.0) // Threshold can be tuned
                    {
                        if (currentGroup.Count > 0)
                            groupedDistances.Add(currentGroup); // Save the current group

                        currentGroup = new List<double>(); // Start a fresh group
                    }
                }

                // Add current point's distance to the active group
                currentGroup.Add(distanceFromOrigin);

                // Update previous point references
                prevX = x;
                prevY = y;
                prevZ = z;
            }

            // Add the last group if it's not empty
            if (currentGroup.Count > 0)
                groupedDistances.Add(currentGroup);

            return groupedDistances;
        }

        private List<(double[] x, double[] y)> ComputeECDFs(IEnumerable<IEnumerable<double>> areaGroups)
        {
            var ecdfList = new List<(double[] x, double[] y)>();

            foreach (var group in areaGroups)
            {
                var groupList = group.ToList();
                int n = groupList.Count;
                var sorted = groupList.OrderBy(v => v).ToArray();
                var cumulative = new double[n];

                for (int i = 0; i < n; i++)
                {
                    cumulative[i] = (i + 1.0) / n;
                }

                ecdfList.Add((sorted, cumulative));
            }

            return ecdfList;
        }

        private void SaveECDFCharts( List<(double[] x, double[] y)> realECDFs, List<(double[] x, double[] y)> simECDFs,List<string> fileIdentifiers, string outputFolder,
                                     string xAxisLabel = "AREA", string filePrefix = "area_")
        {
            Directory.CreateDirectory(outputFolder);

            int groupCount = Math.Min(realECDFs.Count, simECDFs.Count);

            for (int i = 0; i < groupCount; i++)
            {
                var chart = new Chart();
                chart.Size = new Size(800, 600);

                var chartArea = new ChartArea();
                chartArea.AxisX.Title = xAxisLabel;
                chartArea.AxisY.Title = "ECDF";
                chartArea.AxisX.LabelStyle.Format = "F2";

                // Find max across all ECDFs
                double globalMaxX = Math.Max(
                    realECDFs.SelectMany(ecdf => ecdf.x).DefaultIfEmpty(0).Max(),
                    simECDFs.SelectMany(ecdf => ecdf.x).DefaultIfEmpty(0).Max()
                );

                /* To limit the X-Axis to non-zero values in the chart */
                // Combine all X-values from this ECDF pair
                var allXValues = realECDFs[i].x.Concat(simECDFs[i].x).ToList();

                // Filter out values that are effectively irrelevant ( e.g., if you want to ignore zeros)
                var relevantX = allXValues.Where(x => x > 0).ToList();

                if (relevantX.Any())
                {
                    double minX = relevantX.Min();
                    double maxX = relevantX.Max();

                    // Add some buffer (e.g., 5% of the range on each side)
                    double buffer = (maxX - minX) * 0.05;
                    chartArea.AxisX.Minimum = minX - buffer;
                    chartArea.AxisX.Maximum = maxX + buffer;
                }
                else
                {
                    // fallback if all values are zero
                    chartArea.AxisX.Minimum = 0;
                    chartArea.AxisX.Maximum = globalMaxX * 1.2;
                }

                chartArea.AxisY.Minimum = 0;
                chartArea.AxisY.Maximum = 1.2;
                chartArea.AxisY.Interval = 0.2;
                chart.ChartAreas.Add(chartArea);

                var realSeries = new Series($"Real {i}")
                {
                    ChartType = SeriesChartType.Line,
                    Color = Color.Red,
                    BorderWidth = 2
                };

                var simSeries = new Series($"Sim {i}")
                {
                    ChartType = SeriesChartType.Line,
                    Color = Color.Blue,
                    BorderWidth = 2
                };

                for (int j = 0; j < realECDFs[i].x.Length; j++)
                    realSeries.Points.AddXY(realECDFs[i].x[j], realECDFs[i].y[j]);

                for (int j = 0; j < simECDFs[i].x.Length; j++)
                    simSeries.Points.AddXY(simECDFs[i].x[j], simECDFs[i].y[j]);

                chart.Series.Add(realSeries);
                chart.Series.Add(simSeries);

                string imagePath = Path.Combine(outputFolder, $"{filePrefix}ecdfChart_{fileIdentifiers[i]}.png");

                chart.SaveImage(imagePath, ChartImageFormat.Png);
                chart.Dispose();
            }
        }

        private List<(double Bias, double NormalizedBias, double CAVM)> ComputeBiasMetricsPerGroup(List<(double[] x, double[] y)> realECDFs, List<(double[] x, double[] y)> simECDFs, List<List<double>> realValues, List<List<double>> simValues)
        {
            int groupCount = Math.Min(realECDFs.Count, simECDFs.Count);
            var results = new List<(double, double, double)>();

            for (int i = 0; i < groupCount; i++)
            {
                var (realX, realY) = realECDFs[i];
                var (simX, simY) = simECDFs[i];

                double bias = ComputeAVM_Aligned(realX, realY, simX, simY);
                double xRange = realX.Last() - realX.First();
                double normalizedBias = bias / xRange;

                double cavm = ComputeCAVM(realValues[i], simValues[i]);
                results.Add((bias, normalizedBias, cavm));
            }

            return results;
        }

        private double ComputeAVM_Aligned(double[] realX, double[] realY, double[] simX, double[] simY)
        {
            // 1. Combine both X axes into a common set
            HashSet<double> combinedXSet = new HashSet<double>(realX);
            foreach (var x in simX) combinedXSet.Add(x);

            double[] combinedX = combinedXSet.OrderBy(x => x).ToArray();

            // 2. Interpolate both ECDFs to the common X values
            double[] interpRealY = InterpolateECDF(realX, realY, combinedX);
            double[] interpSimY = InterpolateECDF(simX, simY, combinedX);

            // 3. Compute signed area between the two interpolated ECDFs
            double area = 0;
            for (int i = 1; i < combinedX.Length; i++)
            {
                double dx = combinedX[i] - combinedX[i - 1];
                double diff1 = interpRealY[i - 1] - interpSimY[i - 1];
                double diff2 = interpRealY[i] - interpSimY[i];
                area += 0.5 * (diff1 + diff2) * dx; // Trapezoidal rule
            }

            return area;
        }

        private double ComputeCAVM(List<double> realValues, List<double> simValues)
        {
            // Compute ECDFs using the existing ComputeECDFs function
            var realECDF = ComputeECDFs(new List<List<double>> { realValues })[0];
            var simECDF = ComputeECDFs(new List<List<double>> { simValues })[0];

            var (realX, realY) = realECDF;
            var (simX, simY) = simECDF;

            // Compute Bias (AVM)
            double bias = ComputeAVM_Aligned(realX, realY, simX, simY);

            // Shift simulated AREA by -bias
            var shiftedSimValues = simValues.Select(x => x - bias).ToList();

            // Recompute ECDF for corrected simulated data
            var shiftedSimECDF = ComputeECDFs(new List<List<double>> { shiftedSimValues })[0];
            var (shiftedSimX, shiftedSimY) = shiftedSimECDF;

            // Compute CAVM (aligned area again)
            return ComputeAVM_Aligned(realX, realY, shiftedSimX, shiftedSimY);
        }

        private double[] InterpolateECDF(double[] xSource, double[] ySource, double[] xTarget)
        {
            double[] result = new double[xTarget.Length];

            for (int i = 0; i < xTarget.Length; i++)
            {
                double x = xTarget[i];

                // Left extrapolation
                if (x <= xSource[0])
                {
                    result[i] = ySource[0];
                }
                
                // Right extrapolation
                else if (x >= xSource[xSource.Length - 1])
                {
                    result[i] = ySource[ySource.Length - 1];
                }
                else
                {
                    // Find the interval x lies in
                    for (int j = 0; j < xSource.Length - 1; j++)
                    {
                        if (xSource[j] <= x && x <= xSource[j + 1])
                        {
                            double t = (x - xSource[j]) / (xSource[j + 1] - xSource[j]);
                            result[i] = ySource[j] + t * (ySource[j + 1] - ySource[j]);
                            break;
                        }
                    }
                }
            }

            return result;
        }

        /*  
         * Generates a line chart showing detection probability versus distance for Real and Simulated points.
         * For each group of points, the detection probability is computed as: probability = detected_points / expected_points
         * The X-axis represents distance (in meters) and the Y-axis represents detection probability (0 to 1). The chart is saved as a PNG image.
        */
        private void GenerateDetectionProbabilityCharts(string outputImagePath)
        {
            int sensorVariant = comboBox_sensorVariant.SelectedIndex;
            string sensorFolder;
            switch (sensorVariant)
            {
                case 0:
                    sensorFolder = "SMART";
                    break;
                case 1:
                    sensorFolder = "SLIM";
                    break;
                case 2:
                    sensorFolder = "SATELLITE";
                    break;
                default:
                    throw new InvalidOperationException("Unknown sensor variant");
            }

            //This path contains all the panels from 10m to 80m
            var folderPathReal = $@"C:\Projects\Scala3_calibrator_DATA\StatisticsDataForComparison\{sensorFolder}\REAL_AllPanels";


            // Create chart
            var chart = new Chart();
            chart.Size = new System.Drawing.Size(800, 600);
            chart.ChartAreas.Add(new ChartArea());

            // Add detection probability series for REAL data (in red)
            chart.Series.Add(BuildDetectionSeries(folderPathReal, "Real", Color.Red, CalculateRealProbability));

            // Add detection probability series for SIMULATED data (in blue)
            chart.Series.Add(BuildDetectionSeries(folderPathReal, "Simulated", Color.Blue, CalculateSimulatedProbability));

            // Set chart titles and axis labels
            chart.Titles.Add("Detection Probability vs Distance");
            chart.ChartAreas[0].AxisX.Title = "Distance (m)";
            chart.ChartAreas[0].AxisY.Title = "Detection Probability";
            chart.ChartAreas[0].AxisY.Minimum = 0;
            chart.ChartAreas[0].AxisY.Maximum = 1.2;
            chart.ChartAreas[0].AxisY.Interval = 0.2;

            // Add a legend to differentiate series
            chart.Legends.Add(new Legend());

            // Save the chart as PNG image
            chart.SaveImage(outputImagePath, ChartImageFormat.Png);
        }

        /*
         * Builds a data series for the chart by calculating detection probability for each file in the specified folder using the provided calculator function.
        */
        private Series BuildDetectionSeries(string folderPath, string seriesName, Color color, Func<string, int, double> probabilityCalculator)
        {
            var series = new Series(seriesName)
            {
                ChartType = SeriesChartType.Line,
                Color = color,
                BorderWidth = 2
            };

            // Retrieve and sort all text files in the folder
            var files = Directory.GetFiles(folderPath, "*.txt").OrderBy(f => f).ToArray(); 

            // Loop through files and compute detection probability for each
            for (int i = 0; i < files.Length; i++)
            {
                int distance = ExtractDistance(files[i]);
                if (distance < 0) continue;

                double probability = probabilityCalculator(files[i], i);
                series.Points.AddXY(distance, probability);
            }
            return series;
        }

        /*
         * Extracts the distance value from the filename (without extension).
         * Assumes the filename represents a numeric distance.
        */
        private int ExtractDistance(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            return int.TryParse(fileName, out int distance) ? distance : -1;
        }

        //private double CalculateRealProbability(string filePath, int fileIndex)
        //{
        //    var lines = File.ReadAllLines(filePath);
        //    if (lines.Length < 3) return 0.0;

        //    // Parse number of detected points from line 2
        //    if (!int.TryParse(lines[1], out int detectedPoints))
        //        return 0.0;

        //    // Fetch layer and point values from each data line
        //    var dataLines = lines.Skip(2)
        //                         .Select(l => l.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        //                         .ToList();
        //    if (dataLines.Count == 0) return 0.0;

        //    var layers = dataLines.Select(d => double.Parse(d[5])).ToList();
        //    var points = dataLines.Select(d => double.Parse(d[6])).ToList();

        //    double minLayer = layers.Min();
        //    double maxLayer = layers.Max();
        //    double minPoint = points.Min();
        //    double maxPoint = points.Max();

        //    double expectedPoints = ((maxLayer - minLayer) + 1) * ((maxPoint - minPoint) + 1) * 30; // Since 30 files are merged as input, this is a constant
        //    return expectedPoints != 0 ? detectedPoints / expectedPoints : 0.0;
        //}

        private double CalculateRealProbability(string filePath, int fileIndex)
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length < 3) return 0.0;

            // Parse number of detected points from line 2
            if (!int.TryParse(lines[1], out int detectedPoints))
                return 0.0;

            // Read header and determine column indices
            var headerParts = lines[0]
                .Replace("//", "")
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            int layerIndex = Array.IndexOf(headerParts, "Layer");
            int pointIndex = Array.IndexOf(headerParts, "Point");

            if (layerIndex < 0 || pointIndex < 0) return 0.0;

            // Fetch layer and point values from each data line
            var dataLines = lines.Skip(2)
                .Select(l => l.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                .ToList();

            if (dataLines.Count == 0) return 0.0;

            var layers = dataLines.Select(d => double.Parse(d[layerIndex])).ToList();
            var points = dataLines.Select(d => double.Parse(d[pointIndex])).ToList();

            double minLayer = layers.Min();
            double maxLayer = layers.Max();
            double minPoint = points.Min();
            double maxPoint = points.Max();

            double expectedPoints = ((maxLayer - minLayer) + 1) * ((maxPoint - minPoint) + 1) * 30;  // Since 30 files are merged as input, this is a constant

            return expectedPoints != 0 ? detectedPoints / expectedPoints : 0.0;
        }



        private double CalculateSimulatedProbability(string filePath, int fileIndex)
        {
            int matID = 29;
            double[] fixNs = new double[] { -1.0, 0.0, 0.0 };
            double fixedSlantRange = 0.0;
            bool fixSlantRange = false;

            string folderPath = Path.GetDirectoryName(filePath);
            SnippetFolderContents snippetFolderContents_Real = ReadSnippetsFolder(folderPath, matID, fixNs, true);
            fixNs = CalculateSurfaceNormal(snippetFolderContents_Real);
            snippetFolderContents_Real = ReadSnippetsFolder(folderPath, matID, fixNs, true);

            int mergedSimFileCount = 5; // Fetching no.of files merged from panel whihc is ideally 30 takes a lot of time, so choose a smaller number here
            /* get simulated snippet for this file index */
            var snippet_file_Sim = GetSimulatedSnippetFile(snippetFolderContents_Real, fileIndex, mergedSimFileCount, fixNs, fixedSlantRange, fixSlantRange);

            if (snippet_file_Sim.Snippet_Point_List.Count == 0) return 0.0;

            int minLayer = snippet_file_Sim.Snippet_Point_List.Min(p => p.layer);
            int maxLayer = snippet_file_Sim.Snippet_Point_List.Max(p => p.layer);
            int minPoint = snippet_file_Sim.Snippet_Point_List.Min(p => p.pixelInLayer);
            int maxPoint = snippet_file_Sim.Snippet_Point_List.Max(p => p.pixelInLayer);

            int detectedPoints = snippet_file_Sim.Snippet_Point_List.Count;
            int totalExpectedPoints = (maxLayer - minLayer + 1) * (maxPoint - minPoint + 1) * mergedSimFileCount;

            return totalExpectedPoints != 0 ? (double)detectedPoints / totalExpectedPoints : 0.0;
        }
    }
}
