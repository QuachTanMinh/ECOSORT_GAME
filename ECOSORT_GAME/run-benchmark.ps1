# =====================================================================
# Script chạy đo đạc FPS / Frame-time chuẩn hóa cho ECOSORT_GAME Standalone
# Sử dụng:
#   .\run-benchmark.ps1
#   .\run-benchmark.ps1 -Scene campaign
#   .\run-benchmark.ps1 -ExePath "Builds\Windows\ECOSORT_GAME.exe"
# =====================================================================
param(
    [string]$ExePath = "",
    [ValidateSet("kitchen", "campaign")]
    [string]$Scene = "kitchen",
    [string]$OutputFile = "benchmark-result.json"
)

$ErrorActionPreference = "Stop"

# Tự động tìm file thực thi nếu không truyền vào
if (-not $ExePath) {
    $possiblePaths = @(
        "Builds\Windows\ECOSORT_GAME.exe",
        "Builds\Windows\EcoSortQuest.exe",
        "..\ECOSORT_GAME\Builds\Windows\ECOSORT_GAME.exe"
    )
    foreach ($p in $possiblePaths) {
        if (Test-Path $p) {
            $ExePath = $p
            break
        }
    }
    if (-not $ExePath) {
        $found = Get-ChildItem -Path "Builds\Windows" -Filter "*.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) { $ExePath = $found.FullName }
    }
}

if (-not $ExePath -or -not (Test-Path $ExePath)) {
    Write-Error "Không tìm thấy file thực thi tại: $ExePath. Hãy build Windows trước hoặc truyền đường dẫn -ExePath."
    exit 1
}

$OutputPath = [System.IO.Path]::GetFullPath($OutputFile)
if (Test-Path $OutputPath) {
    Remove-Item $OutputPath -Force
}

Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host "  ECOSORT_GAME — BẮT ĐẦU CHẠY BENCHMARK STANDALONE    " -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host "File thực thi : $ExePath"
Write-Host "Cảnh kiểm tra : $Scene"
Write-Host "File kết quả  : $OutputPath"
Write-Host "Đang khởi động game (warmup 5s + đo đạc 12s, tổng ~20s)..." -ForegroundColor Yellow

$proc = Start-Process -FilePath $ExePath -ArgumentList "-ecosort-benchmark", "-ecosort-benchmark-scene", $Scene, "-ecosort-benchmark-output", "`"$OutputPath`"" -PassThru

$timeoutSeconds = 45
$waited = 0
while (-not $proc.HasExited -and $waited -lt $timeoutSeconds) {
    Start-Sleep -Seconds 2
    $waited += 2
}

if (-not $proc.HasExited) {
    Write-Warning "Benchmark quá thời gian chờ ($timeoutSeconds s). Đang đóng process..."
    Stop-Process -Id $proc.Id -Force
}

if (-not (Test-Path $OutputPath)) {
    Write-Error "Không tìm thấy file kết quả sau khi game thoát. Kiểm tra log Unity tại %USERPROFILE%\AppData\LocalLow"
    exit 1
}

$rawJson = Get-Content $OutputPath -Raw
$data = $rawJson | ConvertFrom-Json

Write-Host "`n=======================================================" -ForegroundColor Green
Write-Host "  KẾT QUẢ ĐO ĐẠC HIỆU NĂNG STANDALONE                 " -ForegroundColor Green
Write-Host "=======================================================" -ForegroundColor Green
Write-Host ("GPU            : " + $data.gpu)
Write-Host ("CPU            : " + $data.cpu)
Write-Host ("Độ phân giải   : " + $data.width + " x " + $data.height + " (Toàn màn hình: " + $data.is_fullscreen + ")")
Write-Host ("Giữ Focus      : " + $data.was_focused)
Write-Host ("Tổng số frames : " + $data.frames + " trong " + [Math]::Round($data.seconds, 2) + "s")
Write-Host ("FPS Trung bình : " + [Math]::Round($data.average_fps, 1) + " FPS") -ForegroundColor Cyan
Write-Host ("Frame-time Med : " + [Math]::Round($data.median_ms, 2) + " ms")
Write-Host ("Frame-time P95 : " + [Math]::Round($data.p95_ms, 2) + " ms (1% Low ~ " + [Math]::Round($data.min_fps, 1) + " FPS)")

# Đánh giá tiêu chuẩn
Write-Host "-------------------------------------------------------"
if ($data.median_ms -le 16.67) {
    Write-Host "[ĐẠT - PASS] Hiệu năng xuất sắc (>= 60 FPS mượt mà)" -ForegroundColor Green
} elseif ($data.median_ms -le 33.33) {
    Write-Host "[CHẤP NHẬN ĐƯỢC] Hiệu năng ổn định (30-60 FPS)" -ForegroundColor Yellow
} else {
    Write-Host "[CHƯA ĐẠT] Cần tối ưu thêm (< 30 FPS)" -ForegroundColor Red
}
Write-Host "=======================================================" -ForegroundColor Green
