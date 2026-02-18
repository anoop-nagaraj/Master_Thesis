using System;
using System.Collections.Generic;
using System.Drawing;
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
        int histComp_NumberOfBins = 20;
        int histComp_MaxPeak = 135;
        int histComp_MaxArea = 3000;
        int histComp_MaxWidth = 40;
        bool useLogYAxis = false;

        private void ButtonClick_Run_HistogramComparison(object sender, EventArgs e)
        {
            SetConstants();

            //int sensorVariant = comboBox_sensorVariant.SelectedIndex;

            if (sensorVariant == 0)
            {
                histComp_MaxPeak = 135;
                histComp_MaxArea = 3000;
                histComp_MaxWidth = 40;
            }
            else if (sensorVariant == 1)
            {
                histComp_MaxPeak = 240;
                histComp_MaxArea = 4000;
                histComp_MaxWidth = 40;
            }

            ProcessFolder(sensorVariant, checkBox1_HistogramComparison_Folder1, textBox_HistogramComparison_Folder1Path_SMART, textBox_HistogramComparison_Folder1Path_SLIM, textBox_HistogramComparison_Folder1Path_SATELLITE);
            ProcessFolder(sensorVariant, checkBox2_HistogramComparison_Folder2, textBox_HistogramComparison_Folder2Path_SMART, textBox_HistogramComparison_Folder2Path_SLIM, textBox_HistogramComparison_Folder2Path_SATELLITE);
            ProcessFolder(sensorVariant, checkBox3_HistogramComparison_Folder3, textBox_HistogramComparison_Folder3Path_SMART, textBox_HistogramComparison_Folder3Path_SLIM, textBox_HistogramComparison_Folder3Path_SATELLITE);

            void ProcessFolder(int variant, CheckBox checkBox, TextBox smartPath, TextBox slimPath, TextBox satellitePath)
            {
                if (!checkBox.Checked) return;

                string folderPath = smartPath.Text;
                if (sensorVariant == 0) folderPath = smartPath.Text;
                if (sensorVariant == 1) folderPath = slimPath.Text;
                if (sensorVariant == 2) folderPath = satellitePath.Text;

                string sensorVariantName = "SMART";
                if (sensorVariant == 0) sensorVariantName = "SMART";
                if (sensorVariant == 1) sensorVariantName = "SLIM";
                if (sensorVariant == 2) sensorVariantName = "SATELLITE";

                string folderPath_Real = folderPath + @"REAL\";
                string folderPath_Sim = folderPath + @"SIM\";

                SnippetFolderContents snippetFolderContents_Real = ReadSnippetsFolder(folderPath_Real, -1, new double[] { 1, 1, 1 }, false);
                SnippetFolderContents snippetFolderContents_Sim = ReadSparseFilesFolder(folderPath_Sim);

                string completeFilePath = "C:\\Projects\\Scala3_calibrator_DATA\\SparseDataForComparison\\Output\\" + sensorVariantName + "Histograms Combined.pcd";
                WriteOutputToPCD(snippetFolderContents_Real, snippetFolderContents_Sim, completeFilePath);

                if (checkBox_HistogramComparison_writeOutputToFolder.Checked)
                {
                    SaveSnippetFolderContentsToPNGs(snippetFolderContents_Real, snippetFolderContents_Sim);
                    
                    string PNGsFolderPath = textBox_HistogramComparison_OutputFolderPath.Text + @"\" + sensorVariantName + @"_PNGs\";
                    string summaryPNGFilePath = textBox_HistogramComparison_OutputFolderPath.Text + @"\" + sensorVariantName + @" Histograms.png";
                    ImageCombiner.CombineTilesToOnePng(PNGsFolderPath, summaryPNGFilePath);
                }

                PlotHistogramCharts(snippetFolderContents_Real, snippetFolderContents_Sim);
            }

            void PlotHistogramCharts(SnippetFolderContents real, SnippetFolderContents sim)
            {
                decimal noOfFiles = numeric_LogDistances.Value;

                int[] indices = { 0, 1, 3 };
                Chart[][] charts =
                {
                    new Chart[] { chart_HistogramComparison_1_PEAK, chart_HistogramComparison_1_AREA, chart_HistogramComparison_1_WIDTH },
                    new Chart[] { chart_HistogramComparison_2_PEAK, chart_HistogramComparison_2_AREA, chart_HistogramComparison_2_WIDTH },
                    new Chart[] { chart_HistogramComparison_3_PEAK, chart_HistogramComparison_3_AREA, chart_HistogramComparison_3_WIDTH }
                };

                for (int i = 0; i < indices.Length; i++)
                {
                    int index = indices[i];
                    if (index < real.Snippet_File_List.Count && index < sim.Snippet_File_List.Count)
                    {
                        bool useLogYAxis = index < noOfFiles;
                        SetChartWithData(charts[i][0], real.Snippet_File_List[index], sim.Snippet_File_List[index], "PEAK", useLogYAxis);
                        SetChartWithData(charts[i][1], real.Snippet_File_List[index], sim.Snippet_File_List[index], "AREA", useLogYAxis);
                        SetChartWithData(charts[i][2], real.Snippet_File_List[index], sim.Snippet_File_List[index], "WIDTH", useLogYAxis);
                    }
                }
            }
        }

        void SaveSnippetFolderContentsToPNGs(SnippetFolderContents snippetFolderContents_Real, SnippetFolderContents snippetFolderContents_Sim)
        {
            string sensorVariantName = "SMART";
            if (sensorVariant == 0) sensorVariantName = "SMART";
            if (sensorVariant == 1) sensorVariantName = "SLIM";
            if (sensorVariant == 2) sensorVariantName = "SATELLITE";

            string folderPath = textBox_HistogramComparison_OutputFolderPath.Text + @"\" + sensorVariantName + @"_PNGs\";
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            for (int i = 0; i < snippetFolderContents_Real.Snippet_File_List.Count; i++)
            {
                decimal noOfFiles = numeric_LogDistances.Value;  //Number of files to be displayed in logarithmic Y axis
                useLogYAxis = (i < noOfFiles); // Set to true for the first 'noOfFiles' files, false for the rest

                SetChartWithData(chart_HistogramComparison_1_PEAK, snippetFolderContents_Real.Snippet_File_List[i], snippetFolderContents_Sim.Snippet_File_List[i], "PEAK", useLogYAxis);
                SetChartWithData(chart_HistogramComparison_1_AREA, snippetFolderContents_Real.Snippet_File_List[i], snippetFolderContents_Sim.Snippet_File_List[i], "AREA", useLogYAxis);
                SetChartWithData(chart_HistogramComparison_1_WIDTH, snippetFolderContents_Real.Snippet_File_List[i], snippetFolderContents_Sim.Snippet_File_List[i], "WIDTH", useLogYAxis);

                chart_HistogramComparison_1_PEAK.SaveImage(folderPath + "0_" + i + ".png", ChartImageFormat.Png);
                chart_HistogramComparison_1_AREA.SaveImage(folderPath + "1_" + i + ".png", ChartImageFormat.Png);
                chart_HistogramComparison_1_WIDTH.SaveImage(folderPath + "2_" + i + ".png", ChartImageFormat.Png);
            }
        }

        void SetChartWithData(Chart chart, Snippet_File snippet_File_real, Snippet_File snippet_File_sim, string dataType, bool useLogYAxis)
        {
            Histogram histo_Real = null;
            Histogram histo_Sim = null;

            if (dataType == "PEAK")
            {
                histo_Real = GetPeakHistogram(snippet_File_real);
                histo_Sim = GetPeakHistogram(snippet_File_sim);
            }

            if (dataType == "AREA")
            {
                histo_Real = GetAreaHistogram(snippet_File_real);
                histo_Sim = GetAreaHistogram(snippet_File_sim);
            }

            if (dataType == "WIDTH")
            {
                histo_Real = GetWidthHistogram(snippet_File_real);
                histo_Sim = GetWidthHistogram(snippet_File_sim);
            }

            chart.Series["Real"].Points.Clear();
            chart.Series["Sim"].Points.Clear();
            for (int p = 0; p < histo_Real.numberOfBins; p++) chart.Series["Real"].Points.AddXY(histo_Real.Xvalues[p], histo_Real.Yvalues[p] + 0.1);
            for (int p = 0; p < histo_Sim.numberOfBins; p++) chart.Series["Sim"].Points.AddXY(histo_Sim.Xvalues[p], histo_Sim.Yvalues[p] + 0.1);

            PCCommonFunctions.ResetAxesInChart(chart, false, useLogYAxis);


            Title chartTitle = new Title();
            chartTitle.Text = snippet_File_real.filename + " " + dataType;
            chartTitle.Font = new Font("Arial", 12, FontStyle.Bold);
            chartTitle.ForeColor = Color.Black;
            chartTitle.Alignment = ContentAlignment.MiddleCenter;
            chartTitle.Docking = Docking.Top;
            chart.Titles.Clear();
            chart.Titles.Add(chartTitle);
        }

        Histogram GetPeakHistogram(Snippet_File snippet_File)
        {
            List<double> values = GetPeakValuesList(snippet_File);
            Histogram histogram = new Histogram(values, histComp_NumberOfBins, 0, histComp_MaxPeak);
            return histogram;
        }

        Histogram GetPeakHistogram(Snippet_File snippet_File, float histogramStart, float histogramEnd, int numberOfBins)
        {
            List<double> values = GetPeakValuesList(snippet_File);
            Histogram histogram = new Histogram(values, numberOfBins, histogramStart, histogramEnd);
            return histogram;
        }

        Histogram GetAreaHistogram(Snippet_File snippet_File)
        {
            List<double> values = GetAreaValuesList(snippet_File);
            Histogram histogram = new Histogram(values, histComp_NumberOfBins, 0, histComp_MaxArea);
            return histogram;
        }

        Histogram GetAreaHistogram(Snippet_File snippet_File, float histogramStart, float histogramEnd, int numberOfBins)
        {
            List<double> values = GetAreaValuesList(snippet_File);
            Histogram histogram = new Histogram(values, numberOfBins, histogramStart, histogramEnd);
            return histogram;
        }

        Histogram GetWidthHistogram(Snippet_File snippet_File)
        {
            List<double> values = GetWidthValuesList(snippet_File);
            Histogram histogram = new Histogram(values, histComp_NumberOfBins, 0, histComp_MaxWidth);
            return histogram;
        }

        Histogram GetDistanceHistogram(Snippet_File snippet_File, double histogramStart, double histogramEnd, int numberOfBins)
        {
            List<double> values = GetDistanceValuesList(snippet_File);
            Histogram histogram = new Histogram(values, numberOfBins, histogramStart, histogramEnd);
            return histogram;
        }

        List<double> GetDistanceValuesList(Snippet_File snippet_File)
        {
            List<double> values = new List<double>();
            foreach (var point in snippet_File.Snippet_Point_List) values.Add(point.distance);
            return values;
        }

        List<double> GetPeakValuesList(Snippet_File snippet_File)
        {
            List<double> values = new List<double>();
            foreach (var point in snippet_File.Snippet_Point_List) values.Add(point.PEAK);
            return values;
        }

        List<double> GetAreaValuesList(Snippet_File snippet_File)
        {
            List<double> values = new List<double>();
            foreach (var point in snippet_File.Snippet_Point_List) values.Add(point.AREA);
            return values;
        }

        List<double> GetWidthValuesList(Snippet_File snippet_File)
        {
            List<double> values = new List<double>();
            foreach (var point in snippet_File.Snippet_Point_List) values.Add(point.WIDTH);
            return values;
        }

        class Histogram
        {
            public int numberOfBins;
            public double startValue;
            public double endValue;
            public double binWidth;
            public List<double> Xvalues;
            public List<int> Yvalues;

            public Histogram(List<double> values, int numberOfBins, double startValue, double endValue)
            {
                this.numberOfBins = numberOfBins;
                this.startValue = startValue;
                this.endValue = endValue;

                binWidth = (endValue - startValue) / numberOfBins;
                if (binWidth == 0)
                {
                    binWidth = 1; // Set a default bin width if range is 0
                    this.numberOfBins = 1;
                    numberOfBins = 1;
                }

                // Create bins and count
                var bins = new int[numberOfBins];
                foreach (var value in values)
                {
                    if (value >= startValue && value <= endValue)
                    {
                        int binIndex = Math.Min((int)((value - startValue) / binWidth), numberOfBins - 1);
                        bins[binIndex]++;
                    }
                }
                Yvalues = new List<int>(bins);

                Xvalues = new List<double>();
                for (int i = 0; i < numberOfBins; i++)
                {
                    double Xvalue = startValue + binWidth * (0.5 + i);
                    Xvalues.Add(Xvalue);
                }
            }
        }
    }
}
