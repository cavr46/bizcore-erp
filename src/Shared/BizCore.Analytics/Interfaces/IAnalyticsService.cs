using BizCore.Analytics.Models;
using Microsoft.ML;

namespace BizCore.Analytics.Interfaces;

/// <summary>
/// Core analytics service for ML.NET operations
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Create and configure ML.NET pipeline
    /// </summary>
    Task<MLPipelineResult> CreatePipelineAsync(CreatePipelineRequest request);

    /// <summary>
    /// Train machine learning model
    /// </summary>
    Task<ModelTrainingResult> TrainModelAsync(TrainModelRequest request);

    /// <summary>
    /// Evaluate trained model performance
    /// </summary>
    Task<ModelEvaluationResult> EvaluateModelAsync(EvaluateModelRequest request);

    /// <summary>
    /// Deploy model for predictions
    /// </summary>
    Task<ModelDeploymentResult> DeployModelAsync(DeployModelRequest request);

    /// <summary>
    /// Make predictions using deployed model
    /// </summary>
    Task<PredictionResult> PredictAsync(PredictionRequest request);

    /// <summary>
    /// Make batch predictions
    /// </summary>
    Task<BatchPredictionResult> PredictBatchAsync(BatchPredictionRequest request);

    /// <summary>
    /// Get model performance metrics
    /// </summary>
    Task<ModelMetrics> GetModelMetricsAsync(string modelId, string tenantId);

    /// <summary>
    /// Monitor data drift in deployed models
    /// </summary>
    Task<DataDriftResult> MonitorDataDriftAsync(string modelId, string tenantId);

    /// <summary>
    /// Get model training history
    /// </summary>
    Task<IEnumerable<ModelTraining>> GetTrainingHistoryAsync(string modelId, string tenantId);

    /// <summary>
    /// Get available ML models for tenant
    /// </summary>
    Task<IEnumerable<MLModel>> GetModelsAsync(string tenantId, ModelStatus? status = null);

    /// <summary>
    /// Delete ML model
    /// </summary>
    Task<bool> DeleteModelAsync(string modelId, string tenantId);

    /// <summary>
    /// Export model for external use
    /// </summary>
    Task<byte[]> ExportModelAsync(string modelId, string tenantId, ModelExportFormat format = ModelExportFormat.ONNX);

    /// <summary>
    /// Import pre-trained model
    /// </summary>
    Task<ImportModelResult> ImportModelAsync(ImportModelRequest request);

    /// <summary>
    /// Create analytics experiment
    /// </summary>
    Task<ExperimentResult> CreateExperimentAsync(CreateExperimentRequest request);

    /// <summary>
    /// Run A/B test experiment
    /// </summary>
    Task<ExperimentResult> RunExperimentAsync(string experimentId, string tenantId);

    /// <summary>
    /// Get experiment results
    /// </summary>
    Task<AnalyticsExperiment> GetExperimentAsync(string experimentId, string tenantId);

    /// <summary>
    /// Get business insights from data
    /// </summary>
    Task<BusinessInsights> GetBusinessInsightsAsync(string tenantId, InsightType insightType, DateTime? fromDate = null, DateTime? toDate = null);
}

/// <summary>
/// Data preprocessing service for ML operations
/// </summary>
public interface IDataPreprocessingService
{
    /// <summary>
    /// Clean and prepare dataset for ML training
    /// </summary>
    Task<DataPreprocessingResult> PreprocessDatasetAsync(PreprocessDatasetRequest request);

    /// <summary>
    /// Extract features from raw data
    /// </summary>
    Task<FeatureExtractionResult> ExtractFeaturesAsync(FeatureExtractionRequest request);

    /// <summary>
    /// Normalize numerical features
    /// </summary>
    Task<NormalizationResult> NormalizeDataAsync(NormalizationRequest request);

    /// <summary>
    /// Handle missing values in dataset
    /// </summary>
    Task<MissingValueResult> HandleMissingValuesAsync(MissingValueRequest request);

    /// <summary>
    /// Split dataset into training/validation/test sets
    /// </summary>
    Task<DataSplitResult> SplitDatasetAsync(DataSplitRequest request);

    /// <summary>
    /// Validate data quality
    /// </summary>
    Task<DataQualityResult> ValidateDataQualityAsync(string datasetId, string tenantId);
}

/// <summary>
/// Model training service for specialized ML tasks
/// </summary>
public interface IModelTrainingService
{
    /// <summary>
    /// Train sales forecasting model
    /// </summary>
    Task<ModelTrainingResult> TrainSalesForecastingModelAsync(SalesForecastingRequest request);

    /// <summary>
    /// Train customer churn prediction model
    /// </summary>
    Task<ModelTrainingResult> TrainChurnPredictionModelAsync(ChurnPredictionRequest request);

    /// <summary>
    /// Train demand forecasting model
    /// </summary>
    Task<ModelTrainingResult> TrainDemandForecastingModelAsync(DemandForecastingRequest request);

    /// <summary>
    /// Train customer segmentation model
    /// </summary>
    Task<ModelTrainingResult> TrainCustomerSegmentationModelAsync(CustomerSegmentationRequest request);

    /// <summary>
    /// Train inventory optimization model
    /// </summary>
    Task<ModelTrainingResult> TrainInventoryOptimizationModelAsync(InventoryOptimizationRequest request);

    /// <summary>
    /// Train financial risk assessment model
    /// </summary>
    Task<ModelTrainingResult> TrainRiskAssessmentModelAsync(RiskAssessmentRequest request);

    /// <summary>
    /// Train price optimization model
    /// </summary>
    Task<ModelTrainingResult> TrainPriceOptimizationModelAsync(PriceOptimizationRequest request);

    /// <summary>
    /// Perform automated machine learning (AutoML)
    /// </summary>
    Task<AutoMLResult> RunAutoMLAsync(AutoMLRequest request);
}

/// <summary>
/// Prediction service for real-time and batch predictions
/// </summary>
public interface IPredictionService
{
    /// <summary>
    /// Make real-time prediction
    /// </summary>
    Task<PredictionResult> PredictRealtimeAsync(RealtimePredictionRequest request);

    /// <summary>
    /// Make batch predictions
    /// </summary>
    Task<BatchPredictionResult> PredictBatchAsync(BatchPredictionRequest request);

    /// <summary>
    /// Schedule periodic predictions
    /// </summary>
    Task<ScheduledPredictionResult> SchedulePredictionAsync(SchedulePredictionRequest request);

    /// <summary>
    /// Get prediction explanation
    /// </summary>
    Task<PredictionExplanation> ExplainPredictionAsync(ExplainPredictionRequest request);

    /// <summary>
    /// Monitor prediction performance
    /// </summary>
    Task<PredictionMonitoring> MonitorPredictionsAsync(string modelId, string tenantId, DateTime? fromDate = null);

    /// <summary>
    /// Update model with new data (online learning)
    /// </summary>
    Task<ModelUpdateResult> UpdateModelOnlineAsync(OnlineUpdateRequest request);
}

/// <summary>
/// Model deployment and management service
/// </summary>
public interface IModelDeploymentService
{
    /// <summary>
    /// Deploy model to production environment
    /// </summary>
    Task<DeploymentResult> DeployModelAsync(ModelDeploymentRequest request);

    /// <summary>
    /// Update deployed model
    /// </summary>
    Task<DeploymentResult> UpdateDeploymentAsync(UpdateDeploymentRequest request);

    /// <summary>
    /// Scale model deployment
    /// </summary>
    Task<DeploymentResult> ScaleDeploymentAsync(ScaleDeploymentRequest request);

    /// <summary>
    /// Monitor deployment health
    /// </summary>
    Task<DeploymentHealth> MonitorDeploymentAsync(string deploymentId, string tenantId);

    /// <summary>
    /// Rollback deployment
    /// </summary>
    Task<DeploymentResult> RollbackDeploymentAsync(string deploymentId, string tenantId);

    /// <summary>
    /// Get deployment logs
    /// </summary>
    Task<IEnumerable<DeploymentLog>> GetDeploymentLogsAsync(string deploymentId, string tenantId, int hours = 24);

    /// <summary>
    /// Configure A/B testing for model deployment
    /// </summary>
    Task<ABTestResult> ConfigureABTestAsync(ABTestRequest request);
}