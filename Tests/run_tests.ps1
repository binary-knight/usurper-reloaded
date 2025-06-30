# Usurper Stage 4 - Comprehensive Test Runner
# This script runs all automated tests and generates a detailed report

param(
    [string]$Configuration = "Debug",
    [switch]$SkipBuild,
    [switch]$GenerateReport,
    [switch]$OpenReport,
    [string]$TestFilter = ""
)

Write-Host "🎮 Usurper Reborn - Stage 4 Test Suite Runner" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

# Test categories to run
$TestCategories = @{
    "Core" = @{
        "Name" = "Core Game Mechanics"
        "Description" = "Tests for combat, character progression, and core systems"
        "Filter" = "Category=Core"
    }
    "AI" = @{
        "Name" = "NPC AI System"
        "Description" = "Tests for personality, memory, goals, and decision-making"
        "Filter" = "Category=AI"
    }
    "Integration" = @{
        "Name" = "System Integration"
        "Description" = "Tests for NPC-player interaction and world simulation"
        "Filter" = "Category=Integration"
    }
    "Simulation" = @{
        "Name" = "Emergent Behavior"
        "Description" = "Tests for gang formation, revenge chains, and social dynamics"
        "Filter" = "Category=Simulation"
    }
    "Performance" = @{
        "Name" = "Performance & Stability"
        "Description" = "Tests for memory usage, scaling, and long-running stability"
        "Filter" = "Category=Performance"
    }
}

# Initialize test results
$TestResults = @{}
$OverallStartTime = Get-Date

try {
    # Build solution if not skipping
    if (-not $SkipBuild) {
        Write-Host "🔨 Building solution..." -ForegroundColor Yellow
        $buildResult = dotnet build ../UsurperReborn.sln --configuration $Configuration --verbosity quiet
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Build failed!" -ForegroundColor Red
            exit 1
        }
        Write-Host "✅ Build successful" -ForegroundColor Green
    }

    # Run tests for each category
    foreach ($category in $TestCategories.Keys) {
        $categoryInfo = $TestCategories[$category]
        
        Write-Host ""
        Write-Host "📋 Running $($categoryInfo.Name) Tests" -ForegroundColor Magenta
        Write-Host "   $($categoryInfo.Description)" -ForegroundColor Gray
        Write-Host "   Filter: $($categoryInfo.Filter)" -ForegroundColor Gray
        
        $testStartTime = Get-Date
        
        # Construct test filter
        $filter = $categoryInfo.Filter
        if ($TestFilter) {
            $filter += " & $TestFilter"
        }
        
        # Run tests with detailed output
        $testResult = dotnet test UsurperReborn.Tests.csproj `
            --configuration $Configuration `
            --logger "trx;LogFileName=TestResults_$category.trx" `
            --logger "console;verbosity=normal" `
            --filter $filter `
            --collect:"XPlat Code Coverage" `
            --verbosity quiet
        
        $testEndTime = Get-Date
        $testDuration = $testEndTime - $testStartTime
        
        # Store results
        $TestResults[$category] = @{
            "Name" = $categoryInfo.Name
            "ExitCode" = $LASTEXITCODE
            "Duration" = $testDuration
            "Success" = ($LASTEXITCODE -eq 0)
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ $($categoryInfo.Name) tests passed" -ForegroundColor Green
        } else {
            Write-Host "❌ $($categoryInfo.Name) tests failed" -ForegroundColor Red
        }
        
        Write-Host "   Duration: $($testDuration.TotalSeconds.ToString('F1'))s" -ForegroundColor Gray
    }

    # Generate summary report
    Write-Host ""
    Write-Host "📊 Test Results Summary" -ForegroundColor Cyan
    Write-Host "======================" -ForegroundColor Cyan
    
    $totalTests = $TestResults.Count
    $passedTests = ($TestResults.Values | Where-Object { $_.Success }).Count
    $failedTests = $totalTests - $passedTests
    
    foreach ($category in $TestCategories.Keys) {
        $result = $TestResults[$category]
        $status = if ($result.Success) { "✅ PASS" } else { "❌ FAIL" }
        $color = if ($result.Success) { "Green" } else { "Red" }
        
        Write-Host "$status $($result.Name) ($($result.Duration.TotalSeconds.ToString('F1'))s)" -ForegroundColor $color
    }
    
    Write-Host ""
    Write-Host "Overall Results:" -ForegroundColor White
    Write-Host "  Total Test Categories: $totalTests" -ForegroundColor White
    Write-Host "  Passed: $passedTests" -ForegroundColor Green
    Write-Host "  Failed: $failedTests" -ForegroundColor $(if ($failedTests -eq 0) { "Green" } else { "Red" })
    
    $overallDuration = (Get-Date) - $OverallStartTime
    Write-Host "  Total Time: $($overallDuration.TotalSeconds.ToString('F1'))s" -ForegroundColor White

    # Generate detailed HTML report if requested
    if ($GenerateReport) {
        Write-Host ""
        Write-Host "📋 Generating detailed report..." -ForegroundColor Yellow
        
        $reportPath = "TestReport_$(Get-Date -Format 'yyyyMMdd_HHmmss').html"
        GenerateHtmlReport -TestResults $TestResults -ReportPath $reportPath
        
        Write-Host "✅ Report generated: $reportPath" -ForegroundColor Green
        
        if ($OpenReport) {
            Start-Process $reportPath
        }
    }

    # Display manual testing reminder
    Write-Host ""
    Write-Host "🎯 Next Steps:" -ForegroundColor Cyan
    Write-Host "  1. Review any failed tests above" -ForegroundColor White
    Write-Host "  2. Run manual tests using ManualTestingChecklist.md" -ForegroundColor White
    Write-Host "  3. Test emergent behaviors in actual gameplay" -ForegroundColor White
    Write-Host "  4. Validate performance with larger NPC populations" -ForegroundColor White
    
    # Exit with appropriate code
    if ($failedTests -eq 0) {
        Write-Host ""
        Write-Host "🎉 All automated tests passed!" -ForegroundColor Green
        exit 0
    } else {
        Write-Host ""
        Write-Host "⚠️  Some tests failed. Review results above." -ForegroundColor Yellow
        exit 1
    }

} catch {
    Write-Host ""
    Write-Host "💥 Error running tests: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

function GenerateHtmlReport {
    param (
        [hashtable]$TestResults,
        [string]$ReportPath
    )
    
    $html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Usurper Stage 4 - Test Results Report</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .header { background: #2e8b57; color: white; padding: 20px; border-radius: 5px; }
        .summary { margin: 20px 0; padding: 15px; background: #f5f5f5; border-radius: 5px; }
        .test-category { margin: 10px 0; padding: 15px; border: 1px solid #ddd; border-radius: 5px; }
        .pass { background-color: #d4edda; border-color: #c3e6cb; }
        .fail { background-color: #f8d7da; border-color: #f5c6cb; }
        .checklist { margin: 20px 0; }
        .checklist ul { list-style-type: none; }
        .checklist li { margin: 5px 0; }
        .checklist li:before { content: "☐ "; }
    </style>
</head>
<body>
    <div class="header">
        <h1>🎮 Usurper Stage 4 - Test Results Report</h1>
        <p>Generated on $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')</p>
    </div>
    
    <div class="summary">
        <h2>📊 Summary</h2>
        <p><strong>Total Test Categories:</strong> $($TestResults.Count)</p>
        <p><strong>Passed:</strong> $(($TestResults.Values | Where-Object { $_.Success }).Count)</p>
        <p><strong>Failed:</strong> $(($TestResults.Values | Where-Object { -not $_.Success }).Count)</p>
        <p><strong>Total Duration:</strong> $($overallDuration.TotalSeconds.ToString('F1'))s</p>
    </div>
    
    <h2>🧪 Test Category Results</h2>
"@

    foreach ($category in $TestCategories.Keys) {
        $result = $TestResults[$category]
        $categoryInfo = $TestCategories[$category]
        $cssClass = if ($result.Success) { "pass" } else { "fail" }
        $status = if ($result.Success) { "✅ PASSED" } else { "❌ FAILED" }
        
        $html += @"
    <div class="test-category $cssClass">
        <h3>$status $($result.Name)</h3>
        <p><strong>Description:</strong> $($categoryInfo.Description)</p>
        <p><strong>Duration:</strong> $($result.Duration.TotalSeconds.ToString('F1'))s</p>
        <p><strong>Filter:</strong> $($categoryInfo.Filter)</p>
    </div>
"@
    }

    $html += @"
    
    <div class="checklist">
        <h2>📋 Manual Testing Checklist</h2>
        <p>The following manual tests should be performed to fully validate the NPC AI system:</p>
        
        <h3>🎭 Personality Expression</h3>
        <ul>
            <li>Aggressive NPCs initiate combat frequently</li>
            <li>Greedy NPCs prioritize wealth accumulation</li>
            <li>Social NPCs spend time in taverns</li>
            <li>Cowardly NPCs flee from dangerous situations</li>
            <li>Vengeful NPCs pursue revenge against enemies</li>
        </ul>
        
        <h3>🧠 Memory and Relationships</h3>
        <ul>
            <li>NPCs remember interactions after days/weeks</li>
            <li>Positive interactions build lasting friendships</li>
            <li>Attacks and betrayals create persistent hostility</li>
            <li>Complex relationships develop (mixed feelings)</li>
        </ul>
        
        <h3>🌟 Emergent Behaviors</h3>
        <ul>
            <li>Gang formation occurs naturally</li>
            <li>Revenge chains develop and escalate</li>
            <li>Economic competition creates wealth inequality</li>
            <li>Social hierarchies and friend groups form</li>
            <li>Unique stories emerge each playthrough</li>
        </ul>
        
        <h3>⚡ Performance and Stability</h3>
        <ul>
            <li>Smooth gameplay with 50+ NPCs</li>
            <li>No memory leaks during extended play</li>
            <li>NPCs maintain consistent personalities</li>
            <li>System handles edge cases gracefully</li>
        </ul>
    </div>
    
    <div class="summary">
        <h2>🎯 Recommendations</h2>
        <ol>
            <li><strong>Review Failed Tests:</strong> Address any failing automated tests before proceeding</li>
            <li><strong>Manual Testing:</strong> Complete the manual testing checklist (see ManualTestingChecklist.md)</li>
            <li><strong>Performance Testing:</strong> Test with larger populations (100+ NPCs) in real gameplay</li>
            <li><strong>Emergent Behavior Validation:</strong> Play for several hours to observe natural story emergence</li>
            <li><strong>Player Experience Testing:</strong> Get feedback from multiple players on NPC believability</li>
        </ol>
    </div>
    
    <footer style="margin-top: 40px; padding: 20px; background: #f5f5f5; text-align: center; border-radius: 5px;">
        <p>🎮 Usurper Reborn - Stage 4 Testing Complete</p>
        <p>Remember: Great AI isn't just about passing tests - it's about creating memorable experiences!</p>
    </footer>
</body>
</html>
"@

    $html | Out-File -FilePath $ReportPath -Encoding UTF8
} 