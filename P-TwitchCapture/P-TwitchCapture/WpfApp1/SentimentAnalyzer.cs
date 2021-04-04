﻿using System;
using System.IO;
using Microsoft.ML;
using SentimentAnalysisConsoleApp.DataStructures;
//using Common;
using static Microsoft.ML.DataOperationsCatalog;

namespace PTwitchCapture
{
    class SentimentAnalyzer
    {
        //UNUSED currently
        //We don't use in current experiment
        private static readonly string BaseDatasetsRelativePath = @"../../../../Data";
        private static readonly string DataRelativePath = $"{BaseDatasetsRelativePath}/wikiDetoxAnnotated40kRows.tsv";

        private static readonly string DataPath = GetAbsolutePath(DataRelativePath);

        private static readonly string BaseModelsRelativePath = @"../../../../MLModels";
        private static readonly string ModelRelativePath = $"{BaseModelsRelativePath}/SentimentModel.zip";

        private static readonly string ModelPath = GetAbsolutePath(ModelRelativePath);

        public static string GetAbsolutePath(string relativePath)
        {
            FileInfo _dataRoot = new FileInfo(typeof(SentimentAnalyzer).Assembly.Location);
            string assemblyFolderPath = _dataRoot.Directory.FullName;

            string fullPath = Path.Combine(assemblyFolderPath, relativePath);

            return fullPath;
        }

        private static void TrainData()
        {
            // Create MLContext to be shared across the model creation workflow objects 
            // Set a random seed for repeatable/deterministic results across multiple trainings.
            var mlContext = new MLContext(seed: 1);

            // STEP 1: Common data loading configuration
            IDataView dataView = mlContext.Data.LoadFromTextFile<SentimentIssue>(DataPath, hasHeader: true);

            TrainTestData trainTestSplit = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
            IDataView trainingData = trainTestSplit.TrainSet;
            IDataView testData = trainTestSplit.TestSet;

            // STEP 2: Common data process configuration with pipeline data transformations          
            var dataProcessPipeline = mlContext.Transforms.Text.FeaturizeText(outputColumnName: "Features", inputColumnName: nameof(SentimentIssue.Text));

            // STEP 3: Set the training algorithm, then create and config the modelBuilder                            
            var trainer = mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features");
            var trainingPipeline = dataProcessPipeline.Append(trainer);

            // STEP 4: Train the model fitting to the DataSet
            ITransformer trainedModel = trainingPipeline.Fit(trainingData);

            // STEP 5: Evaluate the model and show accuracy stats
            var predictions = trainedModel.Transform(testData);
            var metrics = mlContext.BinaryClassification.Evaluate(data: predictions, labelColumnName: "Label", scoreColumnName: "Score");

            //ConsoleHelper.PrintBinaryClassificationMetrics(trainer.ToString(), metrics);

            // STEP 6: Save/persist the trained model to a .ZIP file
            mlContext.Model.Save(trainedModel, trainingData.Schema, ModelPath);

            Console.WriteLine("The model is saved to {0}", ModelPath);

            // TRY IT: Make a single test prediction, loading the model from .ZIP file
            SentimentIssue sampleStatement = new SentimentIssue { Text = "U want to get more?" };

            // Create prediction engine related to the loaded trained model
            var predEngine = mlContext.Model.CreatePredictionEngine<SentimentIssue, SentimentPrediction>(trainedModel);

            // Score
            var resultprediction = predEngine.Predict(sampleStatement);

            Console.WriteLine($"=============== Single Prediction  ===============");
            Console.WriteLine($"Text: {sampleStatement.Text} | Prediction: {(Convert.ToBoolean(resultprediction.Prediction) ? "Toxic" : "Non Toxic")} sentiment | Probability of being toxic: {resultprediction.Probability} ");
        }

        private static void TestPredict()
        {

            // Create MLContext
            MLContext mlContext = new MLContext();

            // Define data preparation and trained model schemas
            DataViewSchema modelSchema;

            // Load data preparation pipeline and trained model
            ITransformer trainedModel = mlContext.Model.Load(ModelPath, out modelSchema);

            Console.WriteLine($"=============== Prediction(s)  ===============");

            string[] lines = System.IO.File.ReadAllLines($"{BaseDatasetsRelativePath}/test_predict.txt");

            // TRY IT: Make a single test prediction, loading the model from .ZIP file
            SentimentIssue sampleStatement;

            string[] reviewSentences = { "This is a must place to visit if you are Gundam Lover.", "The rates suck but when you finally get an ssr its the happiest moment ever.", "Also, there is no PvP, making it totally irrelevant whether you have top tier Servants or not.", "Because I like the game, it's that simple.", "I have to go slowly and be patient, and if there's an enemy I can't beat the numbers are so strange and large that I don't normally understand why I'm losing, and I rarely have the time to try again or do something different.",
            "This is shitty game."};

            foreach (string line in lines)
            {
                //assignment to variable
                sampleStatement = new SentimentIssue { Text = line };

                // Create prediction engine related to the loaded trained model
                var predEngine = mlContext.Model.CreatePredictionEngine<SentimentIssue, SentimentPrediction>(trainedModel);

                // Score
                var resultprediction = predEngine.Predict(sampleStatement);

                Console.WriteLine($"Text: {sampleStatement.Text} | Prediction: {(Convert.ToBoolean(resultprediction.Prediction) ? "Toxic" : "Non Toxic")} sentiment | Probability of being toxic: {resultprediction.Probability} ");
            }

        }

        //---------------------------------------

        public static SentimentPrediction predict(String txt)
        {
            MLContext mlContext = new MLContext();
            DataViewSchema modelSchema;
            ITransformer trainedModel = mlContext.Model.Load(ModelPath, out modelSchema);
            //string[] lines = { txt };
            SentimentIssue sampleStatement;
            //foreach (string line in lines)
            //{
            //sampleStatement = new SentimentIssue { Text = line };
            sampleStatement = new SentimentIssue { Text = txt };
            var predEngine = mlContext.Model.CreatePredictionEngine<SentimentIssue, SentimentPrediction>(trainedModel);
            var resultprediction = predEngine.Predict(sampleStatement);
            Console.WriteLine($"Text: {sampleStatement.Text} | Prediction: {(Convert.ToBoolean(resultprediction.Prediction) ? "Toxic" : "Non Toxic")} sentiment | Probability of being toxic: {resultprediction.Probability} ");
            //}
            return resultprediction;
        }
    }
}