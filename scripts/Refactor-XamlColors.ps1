$windowsDir = "d:\D\SAM\3\نظام متكامل لادارة المتجر\GroceryStoreManagement\Windows"
$files = Get-ChildItem -Path $windowsDir -Filter "*.xaml"

$count = 0
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $original = $content

    # Replace hardcoded Green
    $content = $content -replace 'Foreground="Green"', 'Foreground="{StaticResource SuccessBrush}"'
    $content = $content -replace 'Background="Green"', 'Background="{StaticResource SuccessBrush}"'
    
    # Replace hardcoded Red
    $content = $content -replace 'Foreground="Red"', 'Foreground="{StaticResource DangerBrush}"'
    $content = $content -replace 'Background="Red"', 'Background="{StaticResource DangerBrush}"'
    
    # Replace hardcoded gray/hex colors
    $content = $content -replace 'Foreground="#AAA[A-Fa-f0-9]*"', 'Foreground="{StaticResource TextMutedBrush}"'
    $content = $content -replace 'Foreground="#999[A-Fa-f0-9]*"', 'Foreground="{StaticResource TextMutedBrush}"'
    $content = $content -replace 'Foreground="#888[A-Fa-f0-9]*"', 'Foreground="{StaticResource TextMutedBrush}"'
    $content = $content -replace 'Background="#F[0-9A-Fa-f]{5}"', 'Background="{StaticResource SurfaceBrush}"'
    
    # Standardize font families if any remaining
    $content = $content -replace 'FontFamily="Segoe UI"', 'FontFamily="{StaticResource PrimaryFont}"'
    $content = $content -replace 'FontFamily="Tahoma"', 'FontFamily="{StaticResource PrimaryFont}"'
    $content = $content -replace 'FontFamily="Arial"', 'FontFamily="{StaticResource PrimaryFont}"'

    if ($content -cne $original) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
        Write-Host "Updated $($file.Name)"
        $count++
    }
}

Write-Host "Total files updated: $count"
