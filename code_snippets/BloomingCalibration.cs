using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Newtonsoft.Json;


namespace scalagen2fssp_hifi_calibrator
{
    public partial class MainForm : Form
    {
        // Storage for processed data for each distance
        private List<double> curveSim_y1 = new List<double>();
        private List<double> curveSim_z1 = new List<double>();
        private List<double> curveSim_d1 = new List<double>();
        private List<double> curveSim_AREA1 = new List<double>();
        private List<double> curveSim_PEAK1 = new List<double>();
        private List<double> curveSim_WIDTH1 = new List<double>();
        private List<double> curveReal_y1 = new List<double>();
        private List<double> curveReal_z1 = new List<double>();
        private List<double> curveReal_d1 = new List<double>();
        private List<double> curveReal_AREA1 = new List<double>();
        private List<double> curveReal_PEAK1 = new List<double>();
        private List<double> curveReal_WIDTH1 = new List<double>();

        private List<double> curveSim_y2 = new List<double>();
        private List<double> curveSim_z2 = new List<double>();
        private List<double> curveSim_d2 = new List<double>();
        private List<double> curveSim_AREA2 = new List<double>();
        private List<double> curveSim_PEAK2 = new List<double>();
        private List<double> curveSim_WIDTH2 = new List<double>();
        private List<double> curveReal_y2 = new List<double>();
        private List<double> curveReal_z2 = new List<double>();
        private List<double> curveReal_d2 = new List<double>();
        private List<double> curveReal_AREA2 = new List<double>();
        private List<double> curveReal_PEAK2 = new List<double>();
        private List<double> curveReal_WIDTH2 = new List<double>();

        private List<double> curveSim_y3 = new List<double>();
        private List<double> curveSim_z3 = new List<double>();
        private List<double> curveSim_d3 = new List<double>();
        private List<double> curveSim_AREA3 = new List<double>();
        private List<double> curveSim_PEAK3 = new List<double>();
        private List<double> curveSim_WIDTH3 = new List<double>();
        private List<double> curveReal_y3 = new List<double>();
        private List<double> curveReal_z3 = new List<double>();
        private List<double> curveReal_d3 = new List<double>();
        private List<double> curveReal_AREA3 = new List<double>();
        private List<double> curveReal_PEAK3 = new List<double>();
        private List<double> curveReal_WIDTH3 = new List<double>();

        private void button_BloomingCalibrationV_distance1_Click(object sender, EventArgs e)
        {
            SetConstants();
            UpdateCharts(1, "Vertical");
        }


        private void button_BloomingCalibrationV_distance2_Click(object sender, EventArgs e)
        {
            SetConstants();
            UpdateCharts(2, "Vertical");
        }

        private void button_BloomingCalibrationV_distance3_Click(object sender, EventArgs e)
        {
            SetConstants();
            UpdateCharts(3, "Vertical");
        }

        private string GetJsonFilePath(string orientation)
        {
            sensorVariant = comboBox_sensorVariant.SelectedIndex;
            string variantName = "SMART";
            if (sensorVariant == 0) variantName = "SMART";
            if (sensorVariant == 1) variantName = "SLIM";
            if (sensorVariant == 2) variantName = "SATELLITE";

            if (orientation == "Vertical")
            {
                return $@"C:\Projects\Scala3_calibrator_DATA\Blooming\{variantName}\json\Blooming_Calibration_V_{variantName}.json";
            }
            else
            {
                return $@"C:\Projects\Scala3_calibrator_DATA\BloomingH\{variantName}\json\Blooming_Calibration_H_{variantName}.json";
            }
        }

        private void ExecuteWriteJson(string orientation)
        {
            // Determine the prefix based on orientation
            string prefix = orientation == "Vertical" ? "textBox_BloomingCalibrationV_" : "textBox_BloomingCalibrationH_";

            // Create a list to store calibration data from the UI
            var calibrationData = new List<Dictionary<string, object>>();

            // Loop through three sets of input fields (assuming there are three data sets)
            for (int i = 1; i <= 3; i++)
            {
                // Create a dictionary to store data for each dataset
                var dataSet = new Dictionary<string, object>();

                // Retrieve and parse the "Distance" value from the corresponding text box
                dataSet["Distance"] = double.Parse((Controls.Find($"{prefix}distance{i}", true)[0] as TextBox).Text, NumberStyles.Float, CultureInfo.InvariantCulture);

                if (orientation == "Vertical")
                {
                    // For vertical orientation, fetch the values for IndexVerMin, IndexVerMax, SegVerCenter, and SegHor value from the corresponding text box
                    dataSet["IndexVerMin"] = int.Parse((Controls.Find($"{prefix}IndexVerMin{i}", true)[0] as TextBox).Text);
                    dataSet["IndexVerMax"] = int.Parse((Controls.Find($"{prefix}IndexVerMax{i}", true)[0] as TextBox).Text);
                    dataSet["SegVerCenter"] = int.Parse((Controls.Find($"{prefix}SegVar{i}", true)[0] as TextBox).Text);
                    dataSet["SegHor"] = int.Parse((Controls.Find($"{prefix}SegHor{i}", true)[0] as TextBox).Text);
                }
                else if (orientation == "Horizontal")
                {
                    // For horizontal orientation, fetch the values for IndexHorMin, IndexHorMax, SegHorCenter, and SegVer value from the corresponding text box
                    dataSet["IndexHorMin"] = int.Parse((Controls.Find($"{prefix}IndexHorMin{i}", true)[0] as TextBox).Text);
                    dataSet["IndexHorMax"] = int.Parse((Controls.Find($"{prefix}IndexHorMax{i}", true)[0] as TextBox).Text);
                    dataSet["SegHorCenter"] = int.Parse((Controls.Find($"{prefix}SegHorCenter{i}", true)[0] as TextBox).Text);
                    dataSet["SegVer"] = int.Parse((Controls.Find($"{prefix}SegVer{i}", true)[0] as TextBox).Text);
                }

                // Add the dataset to the list
                calibrationData.Add(dataSet);
            }

            // Get the JSON file path based on orientation
            string JsonFilePath = GetJsonFilePath(orientation);

            // Ensure the directory exists before writing the JSON file
            Directory.CreateDirectory(Path.GetDirectoryName(JsonFilePath));

            // Convert the collected data to a formatted JSON string and write it to a file
            File.WriteAllText(JsonFilePath, JsonConvert.SerializeObject(calibrationData, Formatting.Indented));
        }


        private void button_BloomingCalibrationV_writeJson_Click(object sender, EventArgs e)
        {
            ExecuteWriteJson("Vertical");
        }

        // This function loads data from a JSON file and populates the UI text boxes with the values.
        private void LoadBloomingCalibrationJson(string orientation)
        {
            string JsonFilePath = GetJsonFilePath(orientation);

            // Check if the JSON file exists before attempting to read it
            if (File.Exists(JsonFilePath))
            {
                // Read the entire JSON file content as a string
                string jsonContent = File.ReadAllText(JsonFilePath);

                // Deserialize the JSON content into a list of dictionaries
                var calibrationData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonContent);

                // Determine the prefix based on orientation
                string prefix = orientation == "Vertical" ? "textBox_BloomingCalibrationV_" : "textBox_BloomingCalibrationH_";

                // Loop through three sets of input fields and update them with loaded data
                for (int i = 1; i <= 3; i++)
                {
                    // Ensure that the calibration data contains enough entries
                    if (calibrationData.Count >= i)
                    {
                        if (orientation == "Vertical")
                        {
                            // Populate the corresponding text boxes with data from the JSON file
                            Controls.Find($"{prefix}distance{i}", true)[0].Text = calibrationData[i - 1]["Distance"].ToString();

                            Controls.Find($"{prefix}IndexVerMin{i}", true)[0].Text = calibrationData[i - 1]["IndexVerMin"].ToString();

                            Controls.Find($"{prefix}IndexVerMax{i}", true)[0].Text = calibrationData[i - 1]["IndexVerMax"].ToString();

                            Controls.Find($"{prefix}SegVar{i}", true)[0].Text = calibrationData[i - 1]["SegVerCenter"].ToString();

                            Controls.Find($"{prefix}SegHor{i}", true)[0].Text = calibrationData[i - 1]["SegHor"].ToString();
                        }

                        else if (orientation == "Horizontal")
                        {
                            // Populate the corresponding text boxes with data from the JSON file
                            Controls.Find($"{prefix}distance{i}", true)[0].Text = calibrationData[i - 1]["Distance"].ToString();

                            Controls.Find($"{prefix}IndexHorMin{i}", true)[0].Text = calibrationData[i - 1]["IndexHorMin"].ToString();

                            Controls.Find($"{prefix}IndexHorMax{i}", true)[0].Text = calibrationData[i - 1]["IndexHorMax"].ToString();

                            Controls.Find($"{prefix}SegHorCenter{i}", true)[0].Text = calibrationData[i - 1]["SegHorCenter"].ToString();

                            Controls.Find($"{prefix}SegVer{i}", true)[0].Text = calibrationData[i - 1]["SegVer"].ToString();
                        }
                    }
                }
            }
        }

        private void button_BloomingCalibrationV_loadJson_Click(object sender, EventArgs e)
        {
            LoadBloomingCalibrationJson("Vertical");
        }

        void BloomingCalibrationVertical_RunCalibration()
        {
            for (int distanceIndex = 1; distanceIndex <= 3; distanceIndex++)
            {

                // Fetch index range and center values from GUI for each distance (1, 2, 3)
                string boxText = (Controls.Find($"textBox_BloomingCalibrationV_distance{distanceIndex}", true)[0] as TextBox).Text;
                double fixedSlantRange = double.Parse(boxText, NumberStyles.Float, CultureInfo.InvariantCulture);
                int IndexVerMin = int.Parse((Controls.Find($"textBox_BloomingCalibrationV_IndexVerMin{distanceIndex}", true)[0] as TextBox).Text);
                int IndexVerMax = int.Parse((Controls.Find($"textBox_BloomingCalibrationV_IndexVerMax{distanceIndex}", true)[0] as TextBox).Text);
                int cnt_SegVer_center = int.Parse((Controls.Find($"textBox_BloomingCalibrationV_SegVar{distanceIndex}", true)[0] as TextBox).Text);
                int cnt_SegHor = int.Parse((Controls.Find($"textBox_BloomingCalibrationV_SegHor{distanceIndex}", true)[0] as TextBox).Text);


                // Fetch other values
                int IndexHorMin = 0;
                int IndexHorMax = 2550; // POSTPRO_POINTSPERLAYER_COUNT - 1;
                int matID = 29;
                int minHorPointToCalculate = 0;
                int maxHorPointToCalculate = NUM_SLOTS - 1;
                double[] fixNs = new double[3] { -1, 0, 0 };

                // Process the postprocessing input and output
                PostprocessingInput postprocessingInput = FillPostProcessingInputFromBasicInput(fixNs, fixedSlantRange, matID, minHorPointToCalculate, maxHorPointToCalculate, IndexHorMin, IndexHorMax, IndexVerMin, IndexVerMax);
                PostprocessingOutput postprocessingOutput = ProcessScalaFrame(postprocessingInput, true, false);

                string variantName = "SMART";
                if (sensorVariant == 0) variantName = "SMART";
                if (sensorVariant == 1) variantName = "SLIM";
                if (sensorVariant == 2) variantName = "SATELLITE";

                // Define the base path for input files
                string directoryPath = $@"C:\Projects\Scala3_calibrator_DATA\Blooming\"+ variantName + @"\REAL";

                // Get all the .txt files from the directory
                string[] inputFiles = Directory.GetFiles(directoryPath, "*.txt");

                // Select the file based on distanceIndex (1 corresponds to the first file, 2 to the second, etc.)
                string inputFilePath = inputFiles[distanceIndex - 1]; // 0-based index, so subtract 1

                // Get the output file path based on the input file name (same name, but with .pcd extension)
                string completeFilePath = Path.Combine(textBox_BloomingCalibrationV_OutputFolder.Text.Replace("Output", $"{(comboBox_sensorVariant.SelectedIndex == 0 ? "SMART" : "SLIM")}\\Output"), $"{Path.GetFileNameWithoutExtension(inputFilePath)}.pcd");

                // Write the output to a PCD file
                WriteOutputToPCD(postprocessingOutput, completeFilePath);

                // Create lists to store curve data for each distance
                List<double> curveSim_z = new List<double>();
                List<double> curveSim_d = new List<double>();
                List<double> curveSim_AREA = new List<double>();
                List<double> curveSim_PEAK = new List<double>();
                List<double> curveSim_WIDTH = new List<double>();
                List<double> curveReal_z = new List<double>();
                List<double> curveReal_d = new List<double>();
                List<double> curveReal_AREA = new List<double>();
                List<double> curveReal_PEAK = new List<double>();
                List<double> curveReal_WIDTH = new List<double>();

                // Simulated data extraction
                for (int i = 0; i < NUM_LAYERS; i++)
                {
                    int cnt_SegVer = i;

                    for (int j = 0; j < 2; j++) //the data is stored in [0] and sometimes in [1]
                    {
                        double d = postprocessingOutput.sensors[0].layers[i].points[cnt_SegHor].echos[j].DIST;
                        double AREA = postprocessingOutput.sensors[0].layers[i].points[cnt_SegHor].echos[j].AREA;
                        double PEAK = postprocessingOutput.sensors[0].layers[i].points[cnt_SegHor].echos[j].PEAK;
                        double WIDTH = postprocessingOutput.sensors[0].layers[i].points[cnt_SegHor].echos[j].WIDTH;
                        if (d < 400)
                        {
                            curveSim_d.Add(d);
                            curveSim_AREA.Add(AREA);
                            curveSim_PEAK.Add(PEAK);
                            curveSim_WIDTH.Add(WIDTH);

                            Tuple<double, double, double> PixelUnitVector = PCCommonFunctions.PixelUnitVector(cnt_SegHor, cnt_SegVer, NUM_SLOTS, POSTPRO_HFOV, NUM_LAYERS, POSTPRO_VFOV);
                            double z = cnt_SegVer_center - cnt_SegVer;
                            curveSim_z.Add(z);
                        }
                    }
                }

                //Real data extraction
                string[] fileLines = File.ReadAllLines(inputFilePath);
                string headerLine = fileLines[0].TrimStart('/').Trim();
                string[] headers = headerLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                var columnMap = headers.Select((name, index) => new { name, index }).ToDictionary(x => x.name.ToUpper(), x => x.index);

                string[,] PCDFileStringMatrix = PCCommonFunctions.TextFileToStringMatrix(inputFilePath, " "[0], 2);
                float[,] PCDFileFloatMatrix = PCCommonFunctions.StringMatrixToFloatMatrix(PCDFileStringMatrix);

                for (int i = 0; i < PCDFileFloatMatrix.GetLength(0); i++)
                {
                    double x = PCDFileFloatMatrix[i, columnMap["X"]];
                    double y = PCDFileFloatMatrix[i, columnMap["Y"]];
                    double z = PCDFileFloatMatrix[i, columnMap["Z"]];
                    double d = Math.Sqrt(x * x + y * y + z * z);

                    if (d < 400)
                    {
                        curveReal_d.Add(d);
                        if (columnMap.ContainsKey("AREA"))
                            curveReal_AREA.Add(PCDFileFloatMatrix[i, columnMap["AREA"]]);
                        if (columnMap.ContainsKey("PEAK"))
                            curveReal_PEAK.Add(PCDFileFloatMatrix[i, columnMap["PEAK"]]);
                        if (columnMap.ContainsKey("WIDTH"))
                            curveReal_WIDTH.Add(PCDFileFloatMatrix[i, columnMap["WIDTH"]]);
                        if (columnMap.ContainsKey("LAYER"))
                        {
                            double layer = PCDFileFloatMatrix[i, columnMap["LAYER"]];
                            double z_chart = cnt_SegVer_center - layer;
                            curveReal_z.Add(z_chart);
                        }
                    }
                }

                // Store the data for the corresponding distance
                switch (distanceIndex)
                {
                    case 1:
                        curveSim_z1 = new List<double>(curveSim_z);
                        curveSim_d1 = new List<double>(curveSim_d);
                        curveSim_AREA1 = new List<double>(curveSim_AREA);
                        curveSim_PEAK1 = new List<double>(curveSim_PEAK);
                        curveSim_WIDTH1 = new List<double>(curveSim_WIDTH);
                        curveReal_z1 = new List<double>(curveReal_z);
                        curveReal_d1 = new List<double>(curveReal_d);
                        curveReal_AREA1 = new List<double>(curveReal_AREA);
                        curveReal_PEAK1 = new List<double>(curveReal_PEAK);
                        curveReal_WIDTH1 = new List<double>(curveReal_WIDTH);
                        break;
                    case 2:
                        curveSim_z2 = new List<double>(curveSim_z);
                        curveSim_d2 = new List<double>(curveSim_d);
                        curveSim_AREA2 = new List<double>(curveSim_AREA);
                        curveSim_PEAK2 = new List<double>(curveSim_PEAK);
                        curveSim_WIDTH2 = new List<double>(curveSim_WIDTH);
                        curveReal_z2 = new List<double>(curveReal_z);
                        curveReal_d2 = new List<double>(curveReal_d);
                        curveReal_AREA2 = new List<double>(curveReal_AREA);
                        curveReal_PEAK2 = new List<double>(curveReal_PEAK);
                        curveReal_WIDTH2 = new List<double>(curveReal_WIDTH);
                        break;
                    case 3:
                        curveSim_z3 = new List<double>(curveSim_z);
                        curveSim_d3 = new List<double>(curveSim_d);
                        curveSim_AREA3 = new List<double>(curveSim_AREA);
                        curveSim_PEAK3 = new List<double>(curveSim_PEAK);
                        curveSim_WIDTH3 = new List<double>(curveSim_WIDTH);
                        curveReal_z3 = new List<double>(curveReal_z);
                        curveReal_d3 = new List<double>(curveReal_d);
                        curveReal_AREA3 = new List<double>(curveReal_AREA);
                        curveReal_PEAK3 = new List<double>(curveReal_PEAK);
                        curveReal_WIDTH3 = new List<double>(curveReal_WIDTH);
                        break;
                }

                // Update charts after data has been processed
                if(distanceIndex == 1)
                {
                    UpdateCharts(1, "Vertical");
                }
            }
        }

        private void UpdateCharts(int distanceIndex, string orientation)
        {
            int sensorVariant = comboBox_sensorVariant.SelectedIndex;
            bool isVertical = orientation.Equals("vertical", StringComparison.OrdinalIgnoreCase);

            // Declare common lists to store simulation and real measurement data
            List<double> curveSim_Axis = null, curveSim_d = null, curveSim_AREA = null, curveSim_PEAK = null, curveSim_WIDTH = null;
            List<double> curveReal_Axis = null, curveReal_d = null, curveReal_AREA = null, curveReal_PEAK = null, curveReal_WIDTH = null;

            // Axis interval and Y-axis range settings
            int intervalXaxis = 0, distMinY = 0, distMaxY = 0;
            float distIntervalYaxis = 0.0f;

            // Assign appropriate lists based on the orientation and distance index
            switch (distanceIndex)
            {
                case 1:
                    curveSim_Axis = isVertical ? curveSim_z1 : curveSim_y1; // Vertical: Use z-axis, Horizontal: Use y-axis
                    curveSim_d = curveSim_d1;
                    curveSim_AREA = curveSim_AREA1;
                    curveSim_PEAK = curveSim_PEAK1;
                    curveSim_WIDTH = curveSim_WIDTH1;

                    curveReal_Axis = isVertical ? curveReal_z1 : curveReal_y1; // Vertical: Use z-axis, Horizontal: Use y-axis
                    curveReal_d = curveReal_d1;
                    curveReal_AREA = curveReal_AREA1;
                    curveReal_PEAK = curveReal_PEAK1;
                    curveReal_WIDTH = curveReal_WIDTH1;

                    // Determine X-axis interval and Y-axis range based on sensor variant and orientation
                    if (orientation == "Vertical")
                    {
                        intervalXaxis = 40; // Distance between each value on X-Axis
                        (distMinY, distMaxY) = (58, 65); // Set Y-axis min/max based on sensor variant (SMART : SLIM : SATELLITE)
                        if (sensorVariant == 0) (distMinY, distMaxY) = (58, 65);
                        if (sensorVariant == 1) (distMinY, distMaxY) = (68, 72);
                        if (sensorVariant == 2) (distMinY, distMaxY) = (52, 58);
                    }
                    else
                    {
                        double xMin = Math.Min(curveReal_Axis.Min(), curveSim_Axis.Min());
                        double xMax = Math.Max(curveReal_Axis.Max(), curveSim_Axis.Max());
                        intervalXaxis = (int)Math.Ceiling((xMax - xMin) / 10.0); // Divide range into 10 equal parts
                        (distMinY, distMaxY) = sensorVariant == 0 ? (18, 23) : (22, 28); // Set Y-axis min/max based on sensor variant (SMART : SLIM)
                    }
                    distIntervalYaxis = 1.0f;
                    break;

                case 2:
                    curveSim_Axis = isVertical ? curveSim_z2 : curveSim_y2; // Vertical: Use z-axis, Horizontal: Use y-axis
                    curveSim_d = curveSim_d2;
                    curveSim_AREA = curveSim_AREA2;
                    curveSim_PEAK = curveSim_PEAK2;
                    curveSim_WIDTH = curveSim_WIDTH2;
                    curveReal_Axis = isVertical ? curveReal_z2 : curveReal_y2; // Vertical: Use z-axis, Horizontal: Use y-axis
                    curveReal_d = curveReal_d2;
                    curveReal_AREA = curveReal_AREA2;
                    curveReal_PEAK = curveReal_PEAK2;
                    curveReal_WIDTH = curveReal_WIDTH2;

                    // Determine X-axis interval and Y-axis range based on sensor variant and orientation
                    if (orientation == "Vertical")
                    {
                        intervalXaxis = 20; // Distance between each value on X-Axis
                        (distMinY, distMaxY) = (79, 83); // Set Y-axis min/max based on sensor variant (SMART : SLIM : SATELLITE)
                        if (sensorVariant == 0) (distMinY, distMaxY) = (79, 83);
                        if (sensorVariant == 1) (distMinY, distMaxY) = (156, 160);
                        if (sensorVariant == 2) (distMinY, distMaxY) = (148, 160);
                    }
                    else
                    {
                        double xMin = Math.Min(curveReal_Axis.Min(), curveSim_Axis.Min());
                        double xMax = Math.Max(curveReal_Axis.Max(), curveSim_Axis.Max());
                        intervalXaxis = (int)Math.Ceiling((xMax - xMin) / 10.0); // Divide range into 10 equal parts
                        (distMinY, distMaxY) = sensorVariant == 0 ? (32, 38) : (55, 62);
                    }
                    distIntervalYaxis = 1.0f;
                    break;

                case 3:
                    curveSim_Axis = isVertical ? curveSim_z3 : curveSim_y3; // Vertical: Use z-axis, Horizontal: Use y-axis
                    curveSim_d = curveSim_d3;
                    curveSim_AREA = curveSim_AREA3;
                    curveSim_PEAK = curveSim_PEAK3;
                    curveSim_WIDTH = curveSim_WIDTH3;
                    curveReal_Axis = isVertical ? curveReal_z3 : curveReal_y3; // Vertical: Use z-axis, Horizontal: Use y-axis
                    curveReal_d = curveReal_d3;
                    curveReal_AREA = curveReal_AREA3;
                    curveReal_PEAK = curveReal_PEAK3;
                    curveReal_WIDTH = curveReal_WIDTH3;

                    // Determine X-axis interval and Y-axis range based on sensor variant and orientation
                    if (orientation == "Vertical")
                    {
                        intervalXaxis = 10; // Distance between each value on X-Axis
                        (distMinY, distMaxY) = (103, 107); // Set Y-axis min/max based on sensor variant (SMART : SLIM : SATELLITE)
                        if (sensorVariant == 0) (distMinY, distMaxY) = (103, 107);
                        if (sensorVariant == 1) (distMinY, distMaxY) = (199, 202);
                        if (sensorVariant == 2) (distMinY, distMaxY) = (196, 210);
                    }
                    else
                    {
                        double xMin = Math.Min(curveReal_Axis.Min(), curveSim_Axis.Min());
                        double xMax = Math.Max(curveReal_Axis.Max(), curveSim_Axis.Max());
                        intervalXaxis = (int)Math.Ceiling((xMax - xMin) / 10.0); // Divide range into 10 equal parts
                        (distMinY, distMaxY) = sensorVariant == 0 ? (39, 45) : (79, 84); // Set Y-axis min/max based on sensor variant (SMART : SLIM)
                    }
                    distIntervalYaxis = 1.0f;
                    break;
            }

            // Determine correct charts based on orientation
            var chart_Distance = isVertical ? chart_BloomingV_Distance : chart_BloomingH_Distance;
            var chart_AREA = isVertical ? chart_BloomingV_AREA : chart_BloomingH_AREA;
            var chart_PEAK = isVertical ? chart_BloomingV_PEAK : chart_BloomingH_PEAK2;
            var chart_WIDTH = isVertical ? chart_BloomingV_WIDTH : chart_BloomingH_WIDTH;

            // Update all charts
            PCCommonFunctions.ShowDataInChart(chart_Distance, curveSim_Axis, curveSim_d, "Sim", intervalXaxis, distMinY, distMaxY, distIntervalYaxis);
            PCCommonFunctions.ShowDataInChart(chart_Distance, curveReal_Axis, curveReal_d, "Real", intervalXaxis, distMinY, distMaxY, distIntervalYaxis);

            PCCommonFunctions.ShowDataInChart(chart_AREA, curveSim_Axis, curveSim_AREA, "Sim", intervalXaxis);
            PCCommonFunctions.ShowDataInChart(chart_AREA, curveReal_Axis, curveReal_AREA, "Real", intervalXaxis);

            PCCommonFunctions.ShowDataInChart(chart_PEAK, curveSim_Axis, curveSim_PEAK, "Sim", intervalXaxis);
            PCCommonFunctions.ShowDataInChart(chart_PEAK, curveReal_Axis, curveReal_PEAK, "Real", intervalXaxis);

            PCCommonFunctions.ShowDataInChart(chart_WIDTH, curveSim_Axis, curveSim_WIDTH, "Sim", intervalXaxis);
            PCCommonFunctions.ShowDataInChart(chart_WIDTH, curveReal_Axis, curveReal_WIDTH, "Real", intervalXaxis);

            SaveAreaChartImage(chart_AREA, distanceIndex, orientation);

            SaveRightPartOfAreaChart(chart_AREA, distanceIndex, orientation);

        }

        private void button_BloomingCalibrationV_Calibrate_Click(object sender, EventArgs e)
        {
            OpenBloomingCalibrationForm(false); // Vertical calibration
        }

        private void OpenBloomingCalibrationForm(bool isHorizontal)
        {
            // Create an instance of the calibration form
            BloomingCalibrationForm calibrationForm = new BloomingCalibrationForm();

            int sensorVariant = comboBox_sensorVariant.SelectedIndex;
            string configFilePath = @"C:\Projects\scalagen3fssp_hifi_calibrator\bin\ScalaGen3FSSPModel\ValeoScaLaPostprocessingConfig_0.ini";

            if (sensorVariant == 0) configFilePath = @"C:\Projects\scalagen3fssp_hifi_calibrator\bin\ScalaGen3FSSPModel\ValeoScaLaPostprocessingConfig_0.ini";
            if (sensorVariant == 1) configFilePath = @"C:\Projects\scalagen3fssp_hifi_calibrator\bin\ScalaGen3FSSPModel\ValeoScaLaPostprocessingConfig_1.ini";
            if (sensorVariant == 2) configFilePath = @"C:\Projects\scalagen3fssp_hifi_calibrator\bin\ScalaGen3FSSPModel\ValeoScaLaPostprocessingConfig_4.ini";

            calibrationForm.ConfigFilePath = configFilePath;

            // Set horizontal calibration flag
            calibrationForm.IsHorizontalCalibration = isHorizontal;

            // Open the form as a modal dialog
            calibrationForm.ShowDialog();
        }

        private void buttonInfo_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Distance: The object's distance from the sensor.\n\n" +
                "IndexVerMin: Increasing this value shifts the right side of the vertex to the right, and decreasing it shifts it to the left.\n\n" +
                "IndexVerMax: Increasing this value shifts the left side of the vertex to the right, and decreasing it shifts it to the left.\n\n" +
                "SegVer_center: This value is tuned to set the vertex at 0.\n\n" +
                "Seg_Hor: Should match the 'Point' parameter from the Real PCD data.",
                "Information", MessageBoxButtons.OK, MessageBoxIcon.Information
            );
        }
    }
}
