using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Net.Mime.MediaTypeNames;

namespace scalagen2fssp_hifi_calibrator
{
    public partial class MainForm : Form
    {
        // Store right part data for all 3 datasets
        private List<List<double>> rightPartReal_Area_All = new List<List<double>>();
        private List<List<double>> rightPartSim_Area_All = new List<List<double>>();

        public (List<Echo> allEchoSims_dist, List<Echo> allEchoSims_data, int[] pointsPerLayer) ProcessEchoSimulation(Snippet_File Snippet_File, double[] fixNs, double fixedSlantRange, bool fixSlantRange, bool fetchLayerFromFile)
        {
            List<Echo> allEchoSims_dist = new List<Echo>();
            List<Echo> allEchoSims_data = new List<Echo>();
            //int noOfFiles = snippetFolderContents.Snippet_File_List.Count;
            int[] pointsPerLayerSim = Array.Empty<int>();

            //SIM
            //for (int i = 0; i < snippetFolderContents.Snippet_File_List.Count; i++)
            //{
                for (global::System.Int32 j_iteration = 0; j_iteration < 1; j_iteration++)
                {
                    PostprocessingInput postprocessingInput = FillPostProcessingInputforInfiniteSurfaces(Snippet_File, fixNs, fixedSlantRange, fixSlantRange);
                    PostprocessingOutput postprocessingOutput = ProcessScalaFrame(postprocessingInput, true, false);

                    //write data to file
                    if (checkBox_snippets_writeModelOutputIntoFolder_Surf.Checked)
                    {
                        string completeFilePath = textBox_snippets_writeModelOutputIntoFolder_Surf.Text + Snippet_File.filename + "_SIM.pcd";
                        DebugBox.AppendText("writing snippet filename: " + completeFilePath + "\n");

                        WriteOutputToPCD(postprocessingOutput, completeFilePath);
                    }

                    //int sensorVariant = comboBox_sensorVariant.SelectedIndex;

                    //int minHorPointToCalculate = NUM_SLOTS - 1;
                    //if (sensorVariant == 1) minHorPointToCalculate = NUM_SLOTS - 1;
                    //int maxHorPointToCalculate = 0;

                    //int minVerPointToCalculate = NUM_LAYERS - 1;
                    //if (sensorVariant == 1) minVerPointToCalculate = NUM_LAYERS - 1;
                    //int maxVerPointToCalculate = 0;

                    int minHorPointToCalculate = NUM_SLOTS - 1;
                    int maxHorPointToCalculate = 0;
                    int minVerPointToCalculate = NUM_LAYERS - 1;
                    int maxVerPointToCalculate = 0;

                    for (int j = 0; j < Snippet_File.Snippet_Point_List.Count; j++)
                    {
                        int cnt_SegHor = Snippet_File.Snippet_Point_List[j].pixelInLayer;
                        if (cnt_SegHor <= minHorPointToCalculate) minHorPointToCalculate = cnt_SegHor;
                        if (cnt_SegHor >= maxHorPointToCalculate) maxHorPointToCalculate = cnt_SegHor;

                        int cnt_SegVer = Snippet_File.Snippet_Point_List[j].layer;
                        if (cnt_SegVer <= minVerPointToCalculate) minVerPointToCalculate = cnt_SegVer;
                        if (cnt_SegVer >= maxVerPointToCalculate) maxVerPointToCalculate = cnt_SegVer;
                    }

                    // When the Layer and Point data is fetched directly from the *.txt file min/max vertical point can be fetched directly
                    if (fetchLayerFromFile ==  true)
                    {
                        minVerPointToCalculate = 0;
                        maxVerPointToCalculate = NUM_LAYERS - 1;
                        //if (sensorVariant == 1) maxVerPointToCalculate = NUM_LAYERS_1 - 1;
                    }

                    // get average distance
                    for (int p = 0; p < Snippet_File.Snippet_Point_List.Count; p++)
                    {
                        Snippet_Point point = Snippet_File.Snippet_Point_List[p];
                        Echo echoSim = postprocessingOutput.sensors[0].layers[point.layer].points[point.pixelInLayer].echos[0];
                        allEchoSims_dist.Add(echoSim);
                    }

                    // collect data
                    int numLayers = postprocessingOutput.sensors[0].layers.Length;
                    int[] pointsPerLayer = new int[numLayers];
                    for (int i_sim_l = minVerPointToCalculate; i_sim_l <= maxVerPointToCalculate; i_sim_l++)
                    {
                        for (int i_sim_p = minHorPointToCalculate; i_sim_p <= maxHorPointToCalculate; i_sim_p++)
                        {
                            for (int i_echo = 0; i_echo < 3; i_echo++)
                            {
                                // count detected pixels
                                Echo echoSim = postprocessingOutput.sensors[0].layers[i_sim_l].points[i_sim_p].echos[i_echo];
                                if (echoSim.RISING < 400)
                                {
                                    pointsPerLayer[i_sim_l]++;
                                    break;
                                }
                            }

                            for (int i_echo = 0; i_echo < 3; i_echo++)
                            {
                                Echo echoSim = postprocessingOutput.sensors[0].layers[i_sim_l].points[i_sim_p].echos[i_echo];
                                if (echoSim.RISING < 400) allEchoSims_data.Add(echoSim);
                            }
                        }
                    }
                    pointsPerLayerSim = pointsPerLayer; // pointsPerLayerSim accumulates no.of points present in each layer and returns as a list from simulated pcd
               // }
            }
            return (allEchoSims_dist, allEchoSims_data, pointsPerLayerSim);
        }

        private string GenerateResultsSection(List<ReportSection> sections)
        {
            var sb = new StringBuilder();

            // ---- CSS STYLES ----
            sb.AppendLine("<html><head>");
            sb.AppendLine("<style>");
            sb.AppendLine(".label-pass { display: inline-block; background-color: #4CAF50; color: white; padding: 2px 8px; border-radius: 4px; font-weight: bold; }");
            sb.AppendLine(".label-fail { display: inline-block; background-color: #F44336; color: white; padding: 2px 8px; border-radius: 4px; font-weight: bold; }");
            sb.AppendLine(".metrics { margin-bottom: 10px; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head><body>");

            // ---- RESULTS SECTION ----
            sb.AppendLine("<details class='section' open>");
            sb.AppendLine("<summary>Results</summary>");

            // Dummy thresholds (can update later)
            double biasThreshold = 100.0;
            double normBiasThreshold = 0.5;
            double cavmThreshold = 0.01;

            foreach (var section in sections)
            {
                sb.AppendLine("<details class='panel'>");
                sb.AppendLine($"<summary>{section.Title}</summary>");

                // ---- AREA METRICS ----
                if (section.AreaMetrics != null && section.AreaMetrics.Count > 0)
                {
                    sb.AppendLine("<div class='metrics'><b>Area Metrics:</b><br>");
                    for (int i = 0; i < section.AreaMetrics.Count; i++)
                    {
                        var (bias, normalizedBias, cavm) = section.AreaMetrics[i];
                        string datasetName = (section.Panels != null && i < section.Panels.Count)
                            ? section.Panels[i]
                            : $"Dataset {i + 1}";

                        sb.AppendLine($"<div><b>Dataset: {datasetName}</b></div>");

                        double biasDeviation = bias - biasThreshold;
                        double normBiasDeviation = normalizedBias - normBiasThreshold;
                        double cavmDeviation = cavm - cavmThreshold;

                        sb.AppendLine($"Bias: <span class='{(Math.Abs(bias) < biasThreshold ? "label-pass" : "label-fail")}'>{(Math.Abs(bias) < biasThreshold ? "Pass" : "Fail")}</span> (value: {bias:F3}, threshold: {biasThreshold}, deviation: {(biasDeviation >= 0 ? "+" : "")}{biasDeviation:F3})<br>");
                        sb.AppendLine($"Normalized Bias: <span class='{(Math.Abs(normalizedBias) < normBiasThreshold ? "label-pass" : "label-fail")}'>{(Math.Abs(normalizedBias) < normBiasThreshold ? "Pass" : "Fail")}</span> (value: {normalizedBias:F3}, threshold: {normBiasThreshold}, deviation: {(normBiasDeviation >= 0 ? "+" : "")}{normBiasDeviation:F3})<br>");
                        sb.AppendLine($"CAVM: <span class='{(cavm > cavmThreshold ? "label-pass" : "label-fail")}'>{(cavm > cavmThreshold ? "Pass" : "Fail")}</span> (value: {cavm:F3}, threshold: {cavmThreshold}, deviation: {(cavmDeviation >= 0 ? "+" : "")}{cavmDeviation:F3})<br><br>");
                    }
                    sb.AppendLine("</div>");
                }

                // ---- DISTANCE METRICS ----
                if (section.DistanceMetrics != null && section.DistanceMetrics.Count > 0)
                {
                    sb.AppendLine("<div class='metrics'><b>Distance Metrics:</b><br>");
                    for (int i = 0; i < section.DistanceMetrics.Count; i++)
                    {
                        var (bias, normalizedBias, cavm) = section.DistanceMetrics[i];
                        string datasetName = (section.Panels != null && i < section.Panels.Count)
                            ? section.Panels[i]
                            : $"Dataset {i + 1}";

                        sb.AppendLine($"<div><b>Dataset: {datasetName}</b></div>");

                        double biasDeviation = bias - biasThreshold;
                        double normBiasDeviation = normalizedBias - normBiasThreshold;
                        double cavmDeviation = cavm - cavmThreshold;

                        sb.AppendLine($"Bias: <span class='{(Math.Abs(bias) < biasThreshold ? "label-pass" : "label-fail")}'>{(Math.Abs(bias) < biasThreshold ? "Pass" : "Fail")}</span> (value: {bias:F3}, threshold: {biasThreshold}, deviation: {(biasDeviation >= 0 ? "+" : "")}{biasDeviation:F3})<br>");
                        sb.AppendLine($"Normalized Bias: <span class='{(Math.Abs(normalizedBias) < normBiasThreshold ? "label-pass" : "label-fail")}'>{(Math.Abs(normalizedBias) < normBiasThreshold ? "Pass" : "Fail")}</span> (value: {normalizedBias:F3}, threshold: {normBiasThreshold}, deviation: {(normBiasDeviation >= 0 ? "+" : "")}{normBiasDeviation:F3})<br>");
                        sb.AppendLine($"CAVM: <span class='{(cavm > cavmThreshold ? "label-pass" : "label-fail")}'>{(cavm > cavmThreshold ? "Pass" : "Fail")}</span> (value: {cavm:F3}, threshold: {cavmThreshold}, deviation: {(cavmDeviation >= 0 ? "+" : "")}{cavmDeviation:F3})<br><br>");
                    }
                    sb.AppendLine("</div>");
                }

                sb.AppendLine("</details>");
            }

            sb.AppendLine("</details>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        /*
         * Generates an HTML validation report with collapsible sections and panels.
         * Each section contains metrics (area, distance) and related images.
         * The report is saved as "report.html" in the directory chosen via the textBox_location input.
        */
        private void GenerateHtmlReport(List<ReportSection> sections)
        {
            // Build the full path for the report file
            var htmlPath = Path.Combine(textBox_location.Text, "report.html");
            var sb = new StringBuilder();

            // ---- HTML HEAD & STYLES ----
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='en'>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='UTF-8'>");
            sb.AppendLine("<title>Validation Report</title>");

            // Inline CSS for formatting the report
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: sans-serif; padding: 20px; }");
            sb.AppendLine(".title { font-size: 24px; font-weight: bold; margin-bottom: 10px; }");
            sb.AppendLine(".images { display: flex; flex-wrap: wrap; gap: 20px; margin-bottom: 10px; align-items: flex-start; }");
            sb.AppendLine(".images img { max-width: 45%; max-height: 400px; width: auto; height: auto; object-fit: contain; border: 1px solid #ccc; }");
            sb.AppendLine(".metrics { font-size: 16px; line-height: 1.6; margin-bottom: 10px; }");
            sb.AppendLine("hr { margin-top: 30px; }");

            // Collapsible section/panel styling
            sb.AppendLine("details.section { margin: 16px 0; border: 1px solid #e2e2e2; border-radius: 6px; padding: 8px 12px; background: #fafafa; }");
            sb.AppendLine("details.panel { margin: 10px 0; border: 1px dashed #e6e6e6; border-radius: 4px; padding: 6px 10px; background: #fff; }");
            sb.AppendLine("summary { cursor: pointer; user-select: none; }");
            sb.AppendLine(".section > summary { font-size: 20px; font-weight: 600; padding: 4px 0; }");
            sb.AppendLine(".panel > summary { font-size: 16px; font-weight: 600; padding: 2px 0; }");

            // Customize expand/collapse markers
            sb.AppendLine("summary::-webkit-details-marker { display: none; }");
            sb.AppendLine("summary::before { content: '▸ '; }");
            sb.AppendLine("details[open] > summary::before { content: '▾ '; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // ---- REPORT HEADER ----
            sb.AppendLine("<div class='title'>Validation Report</div>");
            sb.AppendLine($"<div>Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}</div>");
            sb.AppendLine("<hr>");

            bool isFirstSection = false; // optionally keep first section expanded

            // ---- RESULTS SECTION ----
            sb.AppendLine(GenerateResultsSection(sections));

            // ---- REPORT SECTIONS ----
            foreach (var section in sections)
            {
                // Start a new collapsible section
                sb.AppendLine($"<details class='section' {(isFirstSection ? "open" : "")}>");
                sb.AppendLine($"<summary>{section.Title}</summary>");

                // Iterate through panels defined for this section
                for (int i = 0; i < section.Panels.Count; i++)
                {
                    var panel = section.Panels[i];

                    sb.AppendLine($"<details class='panel'>");
                    sb.AppendLine($"<summary>Panel: {panel}</summary>");

                    // ---- AREA METRICS ----
                    if (section.AreaMetrics != null && section.AreaMetrics.Count > i)
                    {
                        var metric = section.AreaMetrics[i];

                        // Print numeric values
                        sb.AppendLine("<div class='metrics'><b>Area Metrics:</b><br>");
                        sb.AppendLine($"Bias: {metric.bias:F4}<br>");

                        //Normalised Bias is also shown in % along with its significance
                        string biasInterpretation = metric.normBias >= 0 ? "(Underestimation by the simulation)" : "(Overestimation by the simulation)";
                        double normBiasPercent = metric.normBias * 100;
                        sb.AppendLine($"Normalized Bias: {metric.normBias:F4} → {normBiasPercent:F2}% {biasInterpretation}<br>");

                        //sb.AppendLine($"Normalized Bias: {metric.normBias:F6}<br>");
                        sb.AppendLine($"CAVM: {metric.cavm:F4}</div>");

                        // Add images containing the keyword "area" 
                        if (section.PanelImages.TryGetValue(panel, out var allImages))
                        {
                            var areaImages = allImages
                                .Where(img => Path.GetFileName(img).ToLower().Contains("area"))
                                .ToList();

                            if (areaImages.Count > 0)
                            {
                                sb.AppendLine("<div class='images'>");
                                foreach (var imgPath in areaImages)
                                {
                                    var imgFile = Path.GetFileName(imgPath);
                                    var relativePath = Path.Combine("..", "Images", imgFile).Replace('\\', '/');
                                    sb.AppendLine($"<img src='{relativePath}' alt='Area Image for {panel}'>");
                                }
                                sb.AppendLine("</div>");
                            }
                        }
                    }

                    // ---- DISTANCE METRICS ----
                    if (section.DistanceMetrics != null && section.DistanceMetrics.Count > i)
                    {
                        var metric = section.DistanceMetrics[i];

                        // Print numeric values
                        sb.AppendLine("<div class='metrics'><b>Distance Metrics:</b><br>");
                        sb.AppendLine($"Bias: {metric.bias:F4}<br>");

                        //Normalised Bias is also shown in % along with its significance
                        string biasInterpretation = metric.normBias >= 0 ? "(Underestimation by the simulation)" : "(Overestimation by the simulation)";
                        double normBiasPercent = metric.normBias * 100;
                        sb.AppendLine($"Normalized Bias: {metric.normBias:F4} → {normBiasPercent:F2}% {biasInterpretation}<br>");

                        sb.AppendLine($"CAVM: {metric.cavm:F4}</div>");

                        // Add images containing the keyword "distance"
                        if (section.PanelImages.TryGetValue(panel, out var allImages))
                        {
                            var distanceImages = allImages
                                .Where(img => Path.GetFileName(img).ToLower().Contains("distance"))
                                .ToList();

                            if (distanceImages.Count > 0)
                            {
                                sb.AppendLine("<div class='images'>");
                                foreach (var imgPath in distanceImages)
                                {
                                    var imgFile = Path.GetFileName(imgPath);
                                    var relativePath = Path.Combine("..", "Images", imgFile).Replace('\\', '/');
                                    sb.AppendLine($"<img src='{relativePath}' alt='Distance Image for {panel}'>");
                                }
                                sb.AppendLine("</div>");
                            }
                        }
                    }

                    sb.AppendLine("</details>"); // end panel
                }

                // ---- OPTIONAL: DETECTION PROBABILITY PANEL ----
                if (section.PanelImages.TryGetValue("DetectionProbability", out var detProbImages))
                {
                    sb.AppendLine("<details class='panel'>");
                    sb.AppendLine("<summary>Detection Probability vs Distance</summary>");
                    sb.AppendLine("<div class='images'>");
                    foreach (var imgPath in detProbImages)
                    {
                        var imgFile = Path.GetFileName(imgPath);
                        var relativePath = Path.Combine("..", "Images", imgFile).Replace('\\', '/');
                        sb.AppendLine($"<img src='{relativePath}' alt='Detection Probability Chart'>");
                    }
                    sb.AppendLine("</div>");
                    sb.AppendLine("</details>");
                }

                sb.AppendLine("</details>"); // end section
                isFirstSection = false; // only the very first section could be open
            }

            // ---- CLOSE HTML ----
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            // Ensure the directory exists and write report file
            Directory.CreateDirectory(Path.GetDirectoryName(htmlPath));
            File.WriteAllText(htmlPath, sb.ToString());
        }

        private void SaveAreaChartImage(Chart chart_AREA, int distanceIndex, string orientation)
        {
            string folderPath = @"C:\Projects\Scala3_calibrator_DATA\Images";
            string fileName = $"Blooming_area_{orientation.ToLower()}_{distanceIndex}.png";
            string fullPath = Path.Combine(folderPath, fileName);

            try
            {
                chart_AREA.SaveImage(fullPath, ChartImageFormat.Png);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save chart image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveRightPartOfAreaChart(Chart chart_AREA, int distanceIndex, string orientation)
        {
            // Get the correct dataset based on distanceIndex
            List<double> curveReal_Axis = null, curveSim_Axis = null;
            List<double> curveReal_AREA = null, curveSim_AREA = null;

            switch (distanceIndex)
            {
                case 1:
                    curveReal_Axis = curveReal_z1;
                    curveReal_AREA = curveReal_AREA1;
                    curveSim_Axis = curveSim_z1;
                    curveSim_AREA = curveSim_AREA1;
                    break;
                case 2:
                    curveReal_Axis = curveReal_z2;
                    curveReal_AREA = curveReal_AREA2;
                    curveSim_Axis = curveSim_z2;
                    curveSim_AREA = curveSim_AREA2;
                    break;
                case 3:
                    curveReal_Axis = curveReal_z3;
                    curveReal_AREA = curveReal_AREA3;
                    curveSim_Axis = curveSim_z3;
                    curveSim_AREA = curveSim_AREA3;
                    break;
            }
            if (curveReal_Axis == null || curveSim_Axis == null ||
                curveReal_AREA == null || curveSim_AREA == null ||
                curveReal_Axis.Count == 0 || curveSim_Axis.Count == 0)
                return;

            // Find index of maximum AREA (the peak)
            int peakIndex = curveReal_AREA.IndexOf(curveReal_AREA.Max());

            // Take all points from 0 up to peakIndex (inclusive)
            int startIndex = 0;
            int endIndex = peakIndex;

            var rightPartReal_Axis = curveReal_Axis.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();
            var rightPartReal_Area = curveReal_AREA.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();

            var rightPartSim_Axis = curveSim_Axis.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();
            var rightPartSim_Area = curveSim_AREA.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();

            // Store them for later use
            rightPartReal_Area_All.Add(rightPartReal_Area);
            rightPartSim_Area_All.Add(rightPartSim_Area);

            // Create cropped chart
            Chart croppedChart = new Chart();
            croppedChart.Width = 800;
            croppedChart.Height = 600;
            croppedChart.ChartAreas.Add(new ChartArea("Main"));

            // Real (red)
            var realSeries = new Series("Real");
            realSeries.ChartType = SeriesChartType.Line;
            realSeries.Color = Color.Red;
            realSeries.BorderWidth = 2;
            for (int i = 0; i < rightPartReal_Axis.Count; i++)
                realSeries.Points.AddXY(rightPartReal_Axis[i], rightPartReal_Area[i]);

            // Simulated (blue)
            var simSeries = new Series("Sim");
            simSeries.ChartType = SeriesChartType.Line;
            simSeries.Color = Color.Blue;
            simSeries.BorderWidth = 2;
            for (int i = 0; i < rightPartSim_Axis.Count; i++)
            {
                simSeries.Points.AddXY(rightPartSim_Axis[i], rightPartSim_Area[i]);
            }

            croppedChart.Series.Add(realSeries);
            croppedChart.Series.Add(simSeries);

            // Add a buffer to the left and right margin of x-axis to make the chart look neat
            var area = croppedChart.ChartAreas["Main"];
            area.AxisX.Minimum = rightPartReal_Axis.Min() - 0.02 * (rightPartReal_Axis.Max() - rightPartReal_Axis.Min());
            area.AxisX.Maximum = rightPartReal_Axis.Max() + 0.02 * (rightPartReal_Axis.Max() - rightPartReal_Axis.Min());
            area.RecalculateAxesScale();

            // Add legend
            croppedChart.Legends.Add(new Legend());

            // Save output
            string outputDir = @"C:\Projects\Scala3_calibrator_DATA\Images";
            Directory.CreateDirectory(outputDir);

            string fileName = $"AreaRightPart_{orientation}_Dist{distanceIndex}.png";
            string filePath = Path.Combine(outputDir, fileName);

            croppedChart.SaveImage(filePath, ChartImageFormat.Png);
        }
    }
}
