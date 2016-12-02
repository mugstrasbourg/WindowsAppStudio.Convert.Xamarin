param($installPath, $toolsPath, $package, $project)

$path = [System.IO.Path];

$installFilesPath = $path::Combine($toolsPath, "bin", "Release")

$exePath = $path::Combine($installFilesPath, "WindowsAppStudio.Convert.Xamarin.exe")

$projectPath = $path::GetDirectoryName($project.FullName)

$slnDirectory = $path::GetDirectoryName($projectPath)

iex '& "$($exePath)" -S "$($installFilesPath)" -T "$($slnDirectory)"'