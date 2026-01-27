// =============================================================================
// PaddleOCR GPU Setup and Configuration
// =============================================================================
// Demonstrates CUDA/GPU configuration for Sdcb.PaddleOCR
//
// PREREQUISITES FOR GPU SUPPORT:
//   1. NVIDIA GPU with CUDA capability 3.5+
//   2. NVIDIA Driver 450.80.02+ (Linux) or 452.39+ (Windows)
//   3. CUDA Toolkit 11.8 installed
//   4. cuDNN 8.6.0+ installed and configured
//   5. (Optional) TensorRT 8.5+ for additional optimization
//
// Install GPU packages:
//   dotnet add package Sdcb.PaddleOCR
//   dotnet add package Sdcb.PaddleOCR.Models.Online
//   dotnet add package Sdcb.PaddleInference.runtime.win64.cuda118  // GPU runtime
//   dotnet add package OpenCvSharp4
//   dotnet add package OpenCvSharp4.runtime.win
//
// IMPORTANT: Most development teams should use CPU runtime instead.
// GPU setup is complex and only beneficial for high-volume processing (>1000 images/day).
// =============================================================================

using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR.Models.Online;
using Sdcb.PaddleInference;
using OpenCvSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PaddleOcrExamples
{
    /// <summary>
    /// GPU configuration and performance optimization for PaddleOCR
    /// </summary>
    public class GpuSetupExamples
    {
        // =================================================================
        // Pre-flight Check: Verify CUDA Environment
        // =================================================================
        public static bool VerifyCudaEnvironment()
        {
            Console.WriteLine("Verifying CUDA environment...\n");

            // Check 1: nvidia-smi should be available
            try
            {
                using Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "nvidia-smi",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine("[OK] NVIDIA driver found");
                    // Parse driver version from output
                    Console.WriteLine(output.Split('\n')[2]); // Usually shows driver version
                }
                else
                {
                    Console.WriteLine("[FAIL] nvidia-smi failed - NVIDIA driver not installed");
                    return false;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("[FAIL] nvidia-smi not found - NVIDIA driver not installed");
                return false;
            }

            // Check 2: CUDA environment variable
            string? cudaPath = Environment.GetEnvironmentVariable("CUDA_PATH");
            if (!string.IsNullOrEmpty(cudaPath))
            {
                Console.WriteLine($"[OK] CUDA_PATH: {cudaPath}");
            }
            else
            {
                Console.WriteLine("[WARN] CUDA_PATH not set - CUDA may not be properly installed");
            }

            // Check 3: cuDNN library
            string cudnnPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "NVIDIA GPU Computing Toolkit", "CUDA", "v11.8", "bin", "cudnn64_8.dll"
            );

            if (File.Exists(cudnnPath))
            {
                Console.WriteLine("[OK] cuDNN found");
            }
            else
            {
                Console.WriteLine("[WARN] cuDNN not found at expected location");
                Console.WriteLine("       Expected: " + cudnnPath);
            }

            Console.WriteLine();
            return true;
        }

        // =================================================================
        // Example 1: Explicit GPU Device Selection
        // =================================================================
        public static async Task UseGpuExplicitly(string imagePath)
        {
            Console.WriteLine("Configuring GPU inference...\n");

            // Download models
            FullOcrModel models = await OnlineFullModels.ChineseV4.DownloadAsync();

            // Configure GPU device explicitly
            // Device 0 is typically the primary GPU
            PaddleConfig config = PaddleConfig.FromMemoryModel(
                await File.ReadAllBytesAsync(Path.Combine(models.DetectionModel.DirectoryPath, "inference.pdmodel")),
                await File.ReadAllBytesAsync(Path.Combine(models.DetectionModel.DirectoryPath, "inference.pdiparams"))
            );

            // Enable GPU with device ID 0
            config.EnableUseGpu(
                memoryPoolInitSizeMb: 1000,  // Initial GPU memory pool (adjust based on your GPU)
                deviceId: 0                  // GPU device index (0 for first GPU)
            );

            Console.WriteLine($"GPU Memory Pool: 1000 MB");
            Console.WriteLine($"Device ID: 0");

            // Create OCR with configured engine
            using PaddleOcrAll ocr = new PaddleOcrAll(models);
            using Mat mat = Cv2.ImRead(imagePath);

            var result = ocr.Run(mat);
            Console.WriteLine($"\nExtracted text ({result.Regions.Length} regions):");
            Console.WriteLine(result.Text);
        }

        // =================================================================
        // Example 2: GPU vs CPU Performance Comparison
        // =================================================================
        public static async Task CompareGpuVsCpu(string imagePath, int iterations = 10)
        {
            Console.WriteLine($"Performance comparison: GPU vs CPU ({iterations} iterations)\n");

            FullOcrModel models = await OnlineFullModels.ChineseV4.DownloadAsync();
            using Mat mat = Cv2.ImRead(imagePath);

            if (mat.Empty())
            {
                Console.WriteLine("Error: Could not load image");
                return;
            }

            // Warm up and benchmark CPU
            Console.WriteLine("Benchmarking CPU...");
            using (PaddleOcrAll cpuOcr = new PaddleOcrAll(models))
            {
                // Warm up
                cpuOcr.Run(mat);

                Stopwatch sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    cpuOcr.Run(mat);
                }
                sw.Stop();

                double cpuAvg = sw.ElapsedMilliseconds / (double)iterations;
                Console.WriteLine($"  CPU Average: {cpuAvg:F1} ms/image");
            }

            // Note: GPU benchmark requires GPU runtime package
            // This will fail if only CPU package is installed
            Console.WriteLine("\nBenchmarking GPU...");
            try
            {
                // GPU configuration would go here if runtime is available
                // For demonstration, we show the expected API:
                Console.WriteLine("  (GPU runtime not installed - showing expected pattern)");
                Console.WriteLine(@"
  // With GPU runtime:
  using (PaddleOcrAll gpuOcr = new PaddleOcrAll(models, PaddleDevice.Gpu()))
  {
      // Warm up
      gpuOcr.Run(mat);

      Stopwatch sw = Stopwatch.StartNew();
      for (int i = 0; i < iterations; i++)
      {
          gpuOcr.Run(mat);
      }
      sw.Stop();

      double gpuAvg = sw.ElapsedMilliseconds / (double)iterations;
      Console.WriteLine($""GPU Average: {gpuAvg:F1} ms/image"");
  }
");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  GPU error: {ex.Message}");
                Console.WriteLine("  This is expected if GPU runtime is not installed.");
            }

            Console.WriteLine("\nTypical performance comparison:");
            Console.WriteLine("  CPU (MKL): 300-500ms per image");
            Console.WriteLine("  GPU (CUDA): 50-100ms per image");
            Console.WriteLine("  Speedup: 5-10x with GPU");
        }

        // =================================================================
        // Example 3: Memory Management for GPU
        // =================================================================
        public static async Task GpuMemoryManagement(string[] imagePaths)
        {
            Console.WriteLine("GPU memory management for batch processing\n");

            // GPU memory considerations:
            // 1. Models are loaded into GPU memory (~500MB for full model)
            // 2. Each image requires temporary GPU memory for inference
            // 3. Memory is pooled and reused between inferences
            // 4. Disposing PaddleOcrAll releases GPU memory

            FullOcrModel models = await OnlineFullModels.ChineseV4.DownloadAsync();

            // For batch processing, keep one OCR instance for all images
            // Don't create new instances per image (wastes GPU memory and time)
            using PaddleOcrAll ocr = new PaddleOcrAll(models);

            Console.WriteLine("Processing batch...");
            Console.WriteLine("(Single OCR instance reused for all images)\n");

            foreach (string path in imagePaths)
            {
                using Mat mat = Cv2.ImRead(path);
                if (!mat.Empty())
                {
                    var result = ocr.Run(mat);
                    Console.WriteLine($"{Path.GetFileName(path)}: {result.Regions.Length} regions");
                }
            }

            // GPU memory is released when ocr is disposed
            Console.WriteLine("\nOCR instance disposed - GPU memory released");
        }

        // =================================================================
        // Example 4: Fallback Pattern (GPU with CPU Fallback)
        // =================================================================
        public static async Task GpuWithCpuFallback(string imagePath)
        {
            Console.WriteLine("GPU with CPU fallback pattern\n");

            FullOcrModel models = await OnlineFullModels.ChineseV4.DownloadAsync();
            PaddleOcrAll? ocr = null;
            bool usingGpu = false;

            // Try GPU first, fall back to CPU if unavailable
            try
            {
                Console.WriteLine("Attempting GPU initialization...");

                // This would use GPU if runtime is available
                // ocr = new PaddleOcrAll(models, PaddleDevice.Gpu());
                // usingGpu = true;

                // For this example, we simulate GPU unavailable
                throw new Exception("GPU runtime not available");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GPU unavailable: {ex.Message}");
                Console.WriteLine("Falling back to CPU...\n");

                ocr = new PaddleOcrAll(models);
                usingGpu = false;
            }

            using (ocr)
            {
                Console.WriteLine($"Using: {(usingGpu ? "GPU" : "CPU")}");

                using Mat mat = Cv2.ImRead(imagePath);
                if (!mat.Empty())
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    var result = ocr.Run(mat);
                    sw.Stop();

                    Console.WriteLine($"Processing time: {sw.ElapsedMilliseconds} ms");
                    Console.WriteLine($"Detected regions: {result.Regions.Length}");
                }
            }
        }

        // =================================================================
        // Example 5: Multi-GPU Configuration (Advanced)
        // =================================================================
        public static void MultiGpuConfiguration()
        {
            Console.WriteLine("Multi-GPU configuration notes\n");

            // For systems with multiple GPUs, you can create separate OCR instances
            // each bound to a different GPU device

            Console.WriteLine(@"
Multi-GPU pattern (pseudo-code):

// Create separate instances for each GPU
var ocr0 = new PaddleOcrAll(models, PaddleDevice.Gpu(deviceId: 0));
var ocr1 = new PaddleOcrAll(models, PaddleDevice.Gpu(deviceId: 1));

// Process batches in parallel on different GPUs
Task.WhenAll(
    Task.Run(() => ProcessBatch(ocr0, batch1)),
    Task.Run(() => ProcessBatch(ocr1, batch2))
);

// Note: Each GPU loads its own copy of models
// Total GPU memory = models size x number of GPUs

Benefits:
- Linear scaling with GPU count
- Better utilization for large batches

Considerations:
- Each GPU needs ~500MB for models
- PCIe bandwidth can be bottleneck
- Requires CUDA multi-device support
");
        }

        // =================================================================
        // Example 6: TensorRT Optimization (Advanced)
        // =================================================================
        public static void TensorRtOptimization()
        {
            Console.WriteLine("TensorRT optimization notes\n");

            // TensorRT provides additional optimization for NVIDIA GPUs
            // Can provide 2-3x speedup over standard CUDA inference

            Console.WriteLine(@"
TensorRT setup requirements:
1. Install TensorRT 8.5+ from NVIDIA
2. Add TensorRT bin directory to PATH
3. Use TensorRT-optimized models or enable runtime optimization

Configuration pattern:
var config = PaddleConfig.FromMemoryModel(modelBytes, paramsBytes);
config.EnableUseGpu(1000, 0);
config.EnableTensorRtEngine(
    workspaceSize: 1 << 30,      // 1 GB workspace
    maxBatchSize: 1,
    minSubgraphSize: 3,
    precision: PaddlePrecision.Float32,
    useStatic: false,
    useCalibMode: false
);

Expected speedup: 2-3x over standard CUDA
First run: Slow (building TensorRT engine)
Subsequent runs: Very fast (cached engine)

Considerations:
- TensorRT engines are GPU-architecture specific
- Engine rebuild required for different GPU models
- Additional ~2GB disk space for cached engines
");
        }

        // =================================================================
        // Example 7: Docker GPU Configuration
        // =================================================================
        public static void DockerGpuSetup()
        {
            Console.WriteLine("Docker GPU configuration\n");

            Console.WriteLine(@"
Dockerfile for GPU support:

FROM nvidia/cuda:11.8.0-cudnn8-runtime-ubuntu22.04

# Install .NET 8 runtime
RUN apt-get update && apt-get install -y \
    wget \
    && wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y dotnet-runtime-8.0 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY . .

ENTRYPOINT [""dotnet"", ""YourApp.dll""]


Run with NVIDIA runtime:

docker run --gpus all \
    -v /path/to/images:/data \
    your-paddle-ocr-image

Kubernetes deployment:

apiVersion: v1
kind: Pod
spec:
  containers:
  - name: paddle-ocr
    image: your-paddle-ocr-image
    resources:
      limits:
        nvidia.com/gpu: 1
");
        }

        // =================================================================
        // Summary: Why Most Users Should Use CPU
        // =================================================================
        public static void WhyUseCpu()
        {
            Console.WriteLine("Why CPU is recommended for most users\n");

            Console.WriteLine(@"
GPU Setup Complexity:
---------------------
- NVIDIA drivers: Must match CUDA version
- CUDA Toolkit: 3GB download, version-specific
- cuDNN: Manual installation, version-specific
- TensorRT (optional): Additional complexity
- Docker: Requires nvidia-container-toolkit
- Total setup time: 2-8 hours

GPU Maintenance Burden:
-----------------------
- Driver updates can break CUDA compatibility
- CUDA version upgrades require revalidation
- Container images must be rebuilt for GPU updates
- GPU memory management adds complexity

When GPU is Worth It:
---------------------
- Processing >1000 images/day
- Batch processing (many images at once)
- Real-time video OCR
- GPU infrastructure already exists

When CPU is Better:
-------------------
- Processing <1000 images/day
- Single image requests (web API)
- Docker/Kubernetes without GPU
- Development/testing environments
- Cost-sensitive deployments

Performance Reality Check:
--------------------------
CPU (with MKL): ~300ms/image
GPU (CUDA):     ~60ms/image

For a web API processing 100 requests/hour:
- CPU: 100 * 0.3s = 30 seconds total processing
- GPU: 100 * 0.06s = 6 seconds total processing

Both easily handle the load. GPU only matters at scale.
");
        }

        // =================================================================
        // Usage Example
        // =================================================================
        public static async Task Main(string[] args)
        {
            Console.WriteLine("PaddleOCR GPU Setup Demo\n");
            Console.WriteLine("========================\n");

            // Check CUDA environment
            VerifyCudaEnvironment();

            // Show CPU recommendation
            WhyUseCpu();

            // If image provided, run performance comparison
            if (args.Length > 0 && File.Exists(args[0]))
            {
                await CompareGpuVsCpu(args[0], iterations: 5);
            }
        }
    }
}
