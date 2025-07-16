using Microsoft.ML.Data;
using System.ComponentModel.DataAnnotations;

namespace BizCore.Analytics.Models;

/// <summary>
/// Analytics dataset model for ML.NET processing
/// </summary>
public class AnalyticsDataset
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public DatasetType Type { get; set; } = DatasetType.Sales;
    public DatasetSource Source { get; set; } = new();
    public DatasetSchema Schema { get; set; } = new();
    public DatasetMetrics Metrics { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastProcessed { get; set; }
    public DatasetStatus Status { get; set; } = DatasetStatus.Created;
    public List<DataTransformation> Transformations { get; set; } = new();
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Dataset types for different business domains
/// </summary>
public enum DatasetType
{
    Sales,
    Financial,
    Inventory,
    Customer,
    Marketing,
    Operations,
    Quality,
    Maintenance,
    HR,
    Supply,
    Forecast,
    Risk,
    Custom
}

/// <summary>
/// Dataset status
/// </summary>
public enum DatasetStatus
{
    Created,
    Processing,
    Ready,
    Training,
    Deployed,
    Error,
    Archived
}

/// <summary>
/// Dataset source configuration
/// </summary>
public class DatasetSource
{
    public SourceType Type { get; set; } = SourceType.Database;
    public string ConnectionString { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ApiEndpoint { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromHours(1);
    public bool IsRealTime { get; set; } = false;
}

/// <summary>
/// Source types
/// </summary>
public enum SourceType
{
    Database,
    File,
    API,
    Stream,
    Warehouse,
    Lake,
    Cache,
    Custom
}

/// <summary>
/// Dataset schema definition
/// </summary>
public class DatasetSchema
{
    public List<DataColumn> Columns { get; set; } = new();
    public List<DataRelationship> Relationships { get; set; } = new();
    public List<string> PrimaryKeys { get; set; } = new();
    public List<DataIndex> Indexes { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Data column definition
/// </summary>
public class DataColumn
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DataColumnType Type { get; set; } = DataColumnType.String;
    public bool IsNullable { get; set; } = true;
    public bool IsPrimaryKey { get; set; } = false;
    public bool IsLabel { get; set; } = false;
    public bool IsFeature { get; set; } = true;
    public string Description { get; set; } = string.Empty;
    public object? DefaultValue { get; set; }
    public DataColumnConstraints Constraints { get; set; } = new();
    public Dictionary<string, object> Statistics { get; set; } = new();
}

/// <summary>
/// Data column types
/// </summary>
public enum DataColumnType
{
    String,
    Integer,
    Float,
    Double,
    Boolean,
    DateTime,
    Category,
    Text,
    Image,
    Vector,
    Custom
}

/// <summary>
/// Data column constraints
/// </summary>
public class DataColumnConstraints
{
    public object? MinValue { get; set; }
    public object? MaxValue { get; set; }
    public int? MaxLength { get; set; }
    public string? Pattern { get; set; }
    public List<object> AllowedValues { get; set; } = new();
    public List<string> ValidationRules { get; set; } = new();
}

/// <summary>
/// Data relationship between columns/tables
/// </summary>
public class DataRelationship
{
    public string Name { get; set; } = string.Empty;
    public RelationshipType Type { get; set; } = RelationshipType.OneToMany;
    public string SourceColumn { get; set; } = string.Empty;
    public string TargetColumn { get; set; } = string.Empty;
    public string TargetTable { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = false;
}

/// <summary>
/// Relationship types
/// </summary>
public enum RelationshipType
{
    OneToOne,
    OneToMany,
    ManyToOne,
    ManyToMany
}

/// <summary>
/// Data index definition
/// </summary>
public class DataIndex
{
    public string Name { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
    public bool IsUnique { get; set; } = false;
    public bool IsClustered { get; set; } = false;
    public IndexType Type { get; set; } = IndexType.BTree;
}

/// <summary>
/// Index types
/// </summary>
public enum IndexType
{
    BTree,
    Hash,
    Bitmap,
    ColumnStore,
    FullText,
    Spatial
}

/// <summary>
/// Dataset metrics and statistics
/// </summary>
public class DatasetMetrics
{
    public long RowCount { get; set; }
    public int ColumnCount { get; set; }
    public long SizeBytes { get; set; }
    public double DataQualityScore { get; set; }
    public double CompletenessScore { get; set; }
    public double ConsistencyScore { get; set; }
    public Dictionary<string, double> ColumnMetrics { get; set; } = new();
    public List<DataQualityIssue> QualityIssues { get; set; } = new();
    public DateTime LastCalculated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Data quality issue
/// </summary>
public class DataQualityIssue
{
    public string Type { get; set; } = string.Empty;
    public string Column { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public QualitySeverity Severity { get; set; } = QualitySeverity.Warning;
    public long AffectedRows { get; set; }
    public double Impact { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}

/// <summary>
/// Quality severity levels
/// </summary>
public enum QualitySeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Data transformation definition
/// </summary>
public class DataTransformation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TransformationType Type { get; set; } = TransformationType.Filter;
    public int Order { get; set; }
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string Expression { get; set; } = string.Empty;
    public List<string> InputColumns { get; set; } = new();
    public List<string> OutputColumns { get; set; } = new();
    public TransformationMetrics Metrics { get; set; } = new();
}

/// <summary>
/// Transformation types
/// </summary>
public enum TransformationType
{
    Filter,
    Select,
    Aggregate,
    Join,
    Union,
    Pivot,
    Unpivot,
    Normalize,
    Encode,
    Scale,
    Impute,
    Extract,
    Custom
}

/// <summary>
/// Transformation metrics
/// </summary>
public class TransformationMetrics
{
    public long InputRows { get; set; }
    public long OutputRows { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public double MemoryUsage { get; set; }
    public double DataReduction { get; set; }
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}

/// <summary>
/// ML Model definition
/// </summary>
public class MLModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public MLModelType Type { get; set; } = MLModelType.Regression;
    public MLAlgorithm Algorithm { get; set; } = MLAlgorithm.LinearRegression;
    public string Version { get; set; } = "1.0.0";
    public ModelStatus Status { get; set; } = ModelStatus.Draft;
    public ModelConfiguration Configuration { get; set; } = new();
    public ModelTraining Training { get; set; } = new();
    public ModelEvaluation Evaluation { get; set; } = new();
    public ModelDeployment Deployment { get; set; } = new();
    public ModelMetrics Metrics { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? TrainedAt { get; set; }
    public DateTime? DeployedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// ML model types
/// </summary>
public enum MLModelType
{
    Regression,
    Classification,
    Clustering,
    AnomalyDetection,
    Recommendation,
    Forecasting,
    TextAnalytics,
    ImageClassification,
    DeepLearning,
    Ensemble,
    Custom
}

/// <summary>
/// ML algorithms
/// </summary>
public enum MLAlgorithm
{
    // Regression
    LinearRegression,
    FastTree,
    FastForest,
    LightGbm,
    Gam,
    
    // Classification
    SdcaLogisticRegression,
    FastTreeBinary,
    LightGbmBinary,
    AveragedPerceptron,
    SgdCalibrated,
    
    // Multiclass
    SdcaMaximumEntropy,
    LightGbmMulticlass,
    NaiveBayes,
    OneVersusAll,
    PairwiseCoupling,
    
    // Clustering
    KMeans,
    
    // Anomaly Detection
    RandomizedPca,
    
    // Ranking
    FastTreeRanking,
    LightGbmRanking,
    
    // Forecasting
    FastTreeTweedie,
    SsaForecasting,
    
    // Deep Learning
    ImageClassification,
    
    // Custom
    Custom
}

/// <summary>
/// Model status
/// </summary>
public enum ModelStatus
{
    Draft,
    Training,
    Trained,
    Evaluating,
    Evaluated,
    Deploying,
    Deployed,
    Failed,
    Deprecated,
    Archived
}

/// <summary>
/// Model configuration
/// </summary>
public class ModelConfiguration
{
    public string DatasetId { get; set; } = string.Empty;
    public string LabelColumn { get; set; } = string.Empty;
    public List<string> FeatureColumns { get; set; } = new();
    public List<string> IgnoreColumns { get; set; } = new();
    public double TrainTestSplit { get; set; } = 0.8;
    public int? RandomSeed { get; set; }
    public TrainingParameters TrainingParameters { get; set; } = new();
    public ValidationParameters ValidationParameters { get; set; } = new();
    public Dictionary<string, object> AlgorithmParameters { get; set; } = new();
    public List<string> PreprocessingSteps { get; set; } = new();
    public bool EnableCrossValidation { get; set; } = true;
    public int CrossValidationFolds { get; set; } = 5;
}

/// <summary>
/// Training parameters
/// </summary>
public class TrainingParameters
{
    public int MaxIterations { get; set; } = 100;
    public double LearningRate { get; set; } = 0.1;
    public int BatchSize { get; set; } = 32;
    public double Regularization { get; set; } = 0.01;
    public EarlyStoppingCriteria EarlyStopping { get; set; } = new();
    public bool UseGpu { get; set; } = false;
    public int? MaxMemoryMB { get; set; }
    public TimeSpan? Timeout { get; set; }
}

/// <summary>
/// Early stopping criteria
/// </summary>
public class EarlyStoppingCriteria
{
    public bool IsEnabled { get; set; } = true;
    public string Metric { get; set; } = "Loss";
    public int Patience { get; set; } = 10;
    public double MinDelta { get; set; } = 0.001;
    public EarlyStoppingMode Mode { get; set; } = EarlyStoppingMode.Min;
}

/// <summary>
/// Early stopping modes
/// </summary>
public enum EarlyStoppingMode
{
    Min,
    Max
}

/// <summary>
/// Validation parameters
/// </summary>
public class ValidationParameters
{
    public List<string> Metrics { get; set; } = new();
    public double ValidationSplit { get; set; } = 0.2;
    public int ValidationFrequency { get; set; } = 10;
    public bool ShuffleData { get; set; } = true;
    public bool StratifyData { get; set; } = false;
}

/// <summary>
/// Model training information
/// </summary>
public class ModelTraining
{
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public TrainingStatus Status { get; set; } = TrainingStatus.NotStarted;
    public int CurrentIteration { get; set; }
    public int TotalIterations { get; set; }
    public double CurrentLoss { get; set; }
    public double BestLoss { get; set; }
    public List<TrainingMetric> TrainingHistory { get; set; } = new();
    public List<TrainingMetric> ValidationHistory { get; set; } = new();
    public string LogPath { get; set; } = string.Empty;
    public Dictionary<string, object> TrainingMetrics { get; set; } = new();
}

/// <summary>
/// Training status
/// </summary>
public enum TrainingStatus
{
    NotStarted,
    Running,
    Completed,
    Failed,
    Cancelled,
    Paused
}

/// <summary>
/// Training metric point
/// </summary>
public class TrainingMetric
{
    public int Iteration { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Model evaluation results
/// </summary>
public class ModelEvaluation
{
    public DateTime? EvaluatedAt { get; set; }
    public EvaluationStatus Status { get; set; } = EvaluationStatus.NotStarted;
    public Dictionary<string, double> Metrics { get; set; } = new();
    public ConfusionMatrix? ConfusionMatrix { get; set; }
    public List<FeatureImportance> FeatureImportances { get; set; } = new();
    public ModelBenchmark Benchmark { get; set; } = new();
    public List<EvaluationResult> TestResults { get; set; } = new();
    public string ReportPath { get; set; } = string.Empty;
    public Dictionary<string, object> DetailedMetrics { get; set; } = new();
}

/// <summary>
/// Evaluation status
/// </summary>
public enum EvaluationStatus
{
    NotStarted,
    Running,
    Completed,
    Failed
}

/// <summary>
/// Confusion matrix for classification models
/// </summary>
public class ConfusionMatrix
{
    public int[,] Matrix { get; set; } = new int[0, 0];
    public List<string> Labels { get; set; } = new();
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public Dictionary<string, ClassMetrics> PerClassMetrics { get; set; } = new();
}

/// <summary>
/// Per-class metrics
/// </summary>
public class ClassMetrics
{
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public int Support { get; set; }
}

/// <summary>
/// Feature importance
/// </summary>
public class FeatureImportance
{
    public string FeatureName { get; set; } = string.Empty;
    public double Importance { get; set; }
    public double NormalizedImportance { get; set; }
    public int Rank { get; set; }
}

/// <summary>
/// Model benchmark against baselines
/// </summary>
public class ModelBenchmark
{
    public Dictionary<string, double> BaselineMetrics { get; set; } = new();
    public Dictionary<string, double> ModelMetrics { get; set; } = new();
    public Dictionary<string, double> Improvement { get; set; } = new();
    public BenchmarkResult Result { get; set; } = BenchmarkResult.Better;
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Benchmark results
/// </summary>
public enum BenchmarkResult
{
    Better,
    Worse,
    Similar,
    Inconclusive
}

/// <summary>
/// Evaluation test result
/// </summary>
public class EvaluationResult
{
    public string TestName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public double Score { get; set; }
    public double Threshold { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Model deployment configuration
/// </summary>
public class ModelDeployment
{
    public DateTime? DeployedAt { get; set; }
    public DeploymentStatus Status { get; set; } = DeploymentStatus.NotDeployed;
    public string Environment { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public DeploymentConfiguration Configuration { get; set; } = new();
    public DeploymentMetrics Metrics { get; set; } = new();
    public List<string> Versions { get; set; } = new();
    public string ActiveVersion { get; set; } = string.Empty;
    public DateTime? LastHealthCheck { get; set; }
    public HealthStatus HealthStatus { get; set; } = HealthStatus.Unknown;
}

/// <summary>
/// Deployment status
/// </summary>
public enum DeploymentStatus
{
    NotDeployed,
    Deploying,
    Deployed,
    Failed,
    Updating,
    Scaling,
    Stopped
}

/// <summary>
/// Health status
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

/// <summary>
/// Deployment configuration
/// </summary>
public class DeploymentConfiguration
{
    public int Replicas { get; set; } = 1;
    public string InstanceType { get; set; } = "standard";
    public int CpuCores { get; set; } = 2;
    public int MemoryGB { get; set; } = 4;
    public bool AutoScaling { get; set; } = false;
    public AutoScalingPolicy AutoScalingPolicy { get; set; } = new();
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    public List<string> AllowedOrigins { get; set; } = new();
    public bool RequireAuthentication { get; set; } = true;
    public RateLimitPolicy RateLimit { get; set; } = new();
}

/// <summary>
/// Auto scaling policy
/// </summary>
public class AutoScalingPolicy
{
    public int MinReplicas { get; set; } = 1;
    public int MaxReplicas { get; set; } = 10;
    public double TargetCpuUtilization { get; set; } = 70.0;
    public double TargetMemoryUtilization { get; set; } = 80.0;
    public TimeSpan ScaleUpCooldown { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan ScaleDownCooldown { get; set; } = TimeSpan.FromMinutes(10);
}

/// <summary>
/// Rate limit policy
/// </summary>
public class RateLimitPolicy
{
    public bool IsEnabled { get; set; } = true;
    public int RequestsPerMinute { get; set; } = 1000;
    public int RequestsPerHour { get; set; } = 10000;
    public int RequestsPerDay { get; set; } = 100000;
    public RateLimitAction Action { get; set; } = RateLimitAction.Throttle;
}

/// <summary>
/// Rate limit actions
/// </summary>
public enum RateLimitAction
{
    Allow,
    Throttle,
    Block,
    Queue
}

/// <summary>
/// Deployment metrics
/// </summary>
public class DeploymentMetrics
{
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double SuccessRate { get; set; }
    public double AverageLatency { get; set; }
    public double P95Latency { get; set; }
    public double P99Latency { get; set; }
    public double ThroughputPerSecond { get; set; }
    public double CpuUtilization { get; set; }
    public double MemoryUtilization { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Model metrics and KPIs
/// </summary>
public class ModelMetrics
{
    public ModelPerformance Performance { get; set; } = new();
    public ModelBusiness Business { get; set; } = new();
    public ModelOperational Operational { get; set; } = new();
    public ModelDataDrift DataDrift { get; set; } = new();
    public DateTime LastCalculated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Model performance metrics
/// </summary>
public class ModelPerformance
{
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public double AUC { get; set; }
    public double RMSE { get; set; }
    public double MAE { get; set; }
    public double R2Score { get; set; }
    public Dictionary<string, double> CustomMetrics { get; set; } = new();
}

/// <summary>
/// Model business metrics
/// </summary>
public class ModelBusiness
{
    public double ROI { get; set; }
    public double CostSavings { get; set; }
    public double RevenueImpact { get; set; }
    public double ProcessingCost { get; set; }
    public double MaintenanceCost { get; set; }
    public double BusinessValue { get; set; }
    public Dictionary<string, double> KPIs { get; set; } = new();
}

/// <summary>
/// Model operational metrics
/// </summary>
public class ModelOperational
{
    public double Availability { get; set; }
    public double Reliability { get; set; }
    public double Scalability { get; set; }
    public double Maintainability { get; set; }
    public double SecurityScore { get; set; }
    public double ComplianceScore { get; set; }
    public Dictionary<string, double> SLAMetrics { get; set; } = new();
}

/// <summary>
/// Model data drift detection
/// </summary>
public class ModelDataDrift
{
    public double DriftScore { get; set; }
    public DriftStatus Status { get; set; } = DriftStatus.Stable;
    public DateTime LastDetected { get; set; }
    public List<FeatureDrift> FeatureDrifts { get; set; } = new();
    public DriftAlert Alert { get; set; } = new();
    public Dictionary<string, object> DriftMetrics { get; set; } = new();
}

/// <summary>
/// Drift status
/// </summary>
public enum DriftStatus
{
    Stable,
    Warning,
    Drifting,
    Critical
}

/// <summary>
/// Feature drift information
/// </summary>
public class FeatureDrift
{
    public string FeatureName { get; set; } = string.Empty;
    public double DriftScore { get; set; }
    public DriftStatus Status { get; set; } = DriftStatus.Stable;
    public string DriftType { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Statistics { get; set; } = new();
}

/// <summary>
/// Drift alert configuration
/// </summary>
public class DriftAlert
{
    public bool IsEnabled { get; set; } = true;
    public double Threshold { get; set; } = 0.1;
    public List<string> Recipients { get; set; } = new();
    public string AlertTemplate { get; set; } = string.Empty;
    public AlertFrequency Frequency { get; set; } = AlertFrequency.Immediate;
}

/// <summary>
/// Alert frequency
/// </summary>
public enum AlertFrequency
{
    Immediate,
    Hourly,
    Daily,
    Weekly,
    Custom
}

/// <summary>
/// Prediction request model
/// </summary>
public class PredictionRequest
{
    public string ModelId { get; set; } = string.Empty;
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;
    public Dictionary<string, object> Features { get; set; } = new();
    public List<Dictionary<string, object>> BatchFeatures { get; set; } = new();
    public PredictionOptions Options { get; set; } = new();
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public string RequestedBy { get; set; } = string.Empty;
}

/// <summary>
/// Prediction options
/// </summary>
public class PredictionOptions
{
    public bool IncludeProbabilities { get; set; } = false;
    public bool IncludeFeatureImportance { get; set; } = false;
    public bool IncludeExplanations { get; set; } = false;
    public int TopK { get; set; } = 1;
    public double ConfidenceThreshold { get; set; } = 0.5;
    public string OutputFormat { get; set; } = "json";
    public Dictionary<string, object> CustomOptions { get; set; } = new();
}

/// <summary>
/// Prediction response model
/// </summary>
public class PredictionResponse
{
    public string RequestId { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<PredictionResult> Predictions { get; set; } = new();
    public PredictionMetadata Metadata { get; set; } = new();
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// Prediction result
/// </summary>
public class PredictionResult
{
    public object PredictedValue { get; set; } = new();
    public double Confidence { get; set; }
    public Dictionary<string, double> Probabilities { get; set; } = new();
    public List<FeatureImportance> FeatureImportances { get; set; } = new();
    public List<string> Explanations { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Prediction metadata
/// </summary>
public class PredictionMetadata
{
    public string Algorithm { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public DateTime ModelTrainedAt { get; set; }
    public int FeatureCount { get; set; }
    public List<string> FeatureNames { get; set; } = new();
    public Dictionary<string, object> ModelInfo { get; set; } = new();
    public Dictionary<string, object> RuntimeInfo { get; set; } = new();
}

/// <summary>
/// Analytics experiment for A/B testing and model comparison
/// </summary>
public class AnalyticsExperiment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public ExperimentType Type { get; set; } = ExperimentType.ABTest;
    public ExperimentStatus Status { get; set; } = ExperimentStatus.Draft;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<ExperimentVariant> Variants { get; set; } = new();
    public ExperimentConfiguration Configuration { get; set; } = new();
    public ExperimentResults Results { get; set; } = new();
    public List<string> SuccessMetrics { get; set; } = new();
    public List<string> GuardrailMetrics { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Experiment types
/// </summary>
public enum ExperimentType
{
    ABTest,
    MultiVariate,
    ModelComparison,
    FeatureFlag,
    GradualRollout,
    Custom
}

/// <summary>
/// Experiment status
/// </summary>
public enum ExperimentStatus
{
    Draft,
    Running,
    Paused,
    Completed,
    Cancelled,
    Failed
}

/// <summary>
/// Experiment variant
/// </summary>
public class ExperimentVariant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double TrafficPercentage { get; set; }
    public string ModelId { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public bool IsControl { get; set; } = false;
    public VariantMetrics Metrics { get; set; } = new();
}

/// <summary>
/// Variant metrics
/// </summary>
public class VariantMetrics
{
    public int TotalUsers { get; set; }
    public int ConvertedUsers { get; set; }
    public double ConversionRate { get; set; }
    public double AverageValue { get; set; }
    public double Revenue { get; set; }
    public Dictionary<string, double> CustomMetrics { get; set; } = new();
}

/// <summary>
/// Experiment configuration
/// </summary>
public class ExperimentConfiguration
{
    public TrafficSplitStrategy SplitStrategy { get; set; } = TrafficSplitStrategy.Random;
    public double MinimumDetectableEffect { get; set; } = 0.05;
    public double StatisticalPower { get; set; } = 0.8;
    public double SignificanceLevel { get; set; } = 0.05;
    public int MinimumSampleSize { get; set; } = 1000;
    public Dictionary<string, object> TargetingRules { get; set; } = new();
    public bool EnableEarlyStop { get; set; } = true;
    public Dictionary<string, object> EarlyStopRules { get; set; } = new();
}

/// <summary>
/// Traffic split strategies
/// </summary>
public enum TrafficSplitStrategy
{
    Random,
    UserHash,
    Geographic,
    Temporal,
    Custom
}

/// <summary>
/// Experiment results
/// </summary>
public class ExperimentResults
{
    public DateTime? CompletedAt { get; set; }
    public string WinningVariant { get; set; } = string.Empty;
    public double StatisticalSignificance { get; set; }
    public double Effect { get; set; }
    public double ConfidenceInterval { get; set; }
    public Dictionary<string, VariantComparison> Comparisons { get; set; } = new();
    public List<string> Insights { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Variant comparison results
/// </summary>
public class VariantComparison
{
    public string VariantA { get; set; } = string.Empty;
    public string VariantB { get; set; } = string.Empty;
    public double Lift { get; set; }
    public double PValue { get; set; }
    public bool IsSignificant { get; set; }
    public string ConfidenceInterval { get; set; } = string.Empty;
    public Dictionary<string, double> MetricComparisons { get; set; } = new();
}

/// <summary>
/// ML.NET specific data models for training
/// </summary>
public class SalesData
{
    [LoadColumn(0)]
    public float Date { get; set; }

    [LoadColumn(1)]
    public float ProductId { get; set; }

    [LoadColumn(2)]
    public float Quantity { get; set; }

    [LoadColumn(3)]
    public float Price { get; set; }

    [LoadColumn(4)]
    public float CustomerId { get; set; }

    [LoadColumn(5)]
    public float StoreId { get; set; }

    [LoadColumn(6)]
    public float CategoryId { get; set; }

    [LoadColumn(7)]
    public float SeasonalFactor { get; set; }

    [LoadColumn(8)]
    [ColumnName("Label")]
    public float Sales { get; set; }
}

/// <summary>
/// Sales prediction model
/// </summary>
public class SalesPrediction
{
    [ColumnName("Score")]
    public float PredictedSales { get; set; }
}

/// <summary>
/// Customer data for churn prediction
/// </summary>
public class CustomerData
{
    [LoadColumn(0)]
    public float CustomerId { get; set; }

    [LoadColumn(1)]
    public float Age { get; set; }

    [LoadColumn(2)]
    public float Income { get; set; }

    [LoadColumn(3)]
    public float TotalSpent { get; set; }

    [LoadColumn(4)]
    public float OrderFrequency { get; set; }

    [LoadColumn(5)]
    public float DaysSinceLastOrder { get; set; }

    [LoadColumn(6)]
    public float SupportTickets { get; set; }

    [LoadColumn(7)]
    public float Tenure { get; set; }

    [LoadColumn(8)]
    [ColumnName("Label")]
    public bool Churned { get; set; }
}

/// <summary>
/// Customer churn prediction
/// </summary>
public class CustomerChurnPrediction
{
    [ColumnName("PredictedLabel")]
    public bool WillChurn { get; set; }

    [ColumnName("Probability")]
    public float ChurnProbability { get; set; }

    [ColumnName("Score")]
    public float Score { get; set; }
}

/// <summary>
/// Inventory data for demand forecasting
/// </summary>
public class InventoryData
{
    [LoadColumn(0)]
    public float Date { get; set; }

    [LoadColumn(1)]
    public float ProductId { get; set; }

    [LoadColumn(2)]
    public float CurrentStock { get; set; }

    [LoadColumn(3)]
    public float LeadTime { get; set; }

    [LoadColumn(4)]
    public float SeasonalIndex { get; set; }

    [LoadColumn(5)]
    public float Promotions { get; set; }

    [LoadColumn(6)]
    public float CompetitorPrice { get; set; }

    [LoadColumn(7)]
    [ColumnName("Label")]
    public float Demand { get; set; }
}

/// <summary>
/// Demand forecast prediction
/// </summary>
public class DemandForecast
{
    [ColumnName("Score")]
    public float PredictedDemand { get; set; }
}

/// <summary>
/// Request/Response models for API
/// </summary>
public class CreateMLModelRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string TenantId { get; set; } = string.Empty;
    
    [Required]
    public MLModelType Type { get; set; }
    
    [Required]
    public MLAlgorithm Algorithm { get; set; }
    
    [Required]
    public string DatasetId { get; set; } = string.Empty;
    
    [Required]
    public string LabelColumn { get; set; } = string.Empty;
    
    public List<string> FeatureColumns { get; set; } = new();
    
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Analytics result wrapper
/// </summary>
public class AnalyticsResult
{
    public bool IsSuccess { get; set; }
    public object? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ValidationError> ValidationErrors { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static AnalyticsResult Success(object? data = null) =>
        new() { IsSuccess = true, Data = data };

    public static AnalyticsResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };

    public static AnalyticsResult ValidationFailure(List<ValidationError> errors) =>
        new() { IsSuccess = false, ValidationErrors = errors };
}

/// <summary>
/// Validation error
/// </summary>
public class ValidationError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Property { get; set; }
    public object? AttemptedValue { get; set; }
}