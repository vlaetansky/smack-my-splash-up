using ICSharpCode.SharpZipLib.Zip;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace Keroosha.SuckMySplashUp.Patcher;

public class Patcher
{
    public string? BinaryPath { get;  }
    public string? SplashPathx2 { get; set; }
    public string? SplashPath { get; set; }

    private string OldFilePath => $"{BinaryPath}.old";
    
    public Patcher(string? binaryPath, string? splashPathx2, string? splashPath = null)
    {
        BinaryPath = binaryPath;
        SplashPathx2 = splashPathx2;
        SplashPath = splashPath;
    }

    private void EnsurePatcherValid()
    {
        if (string.IsNullOrWhiteSpace(BinaryPath)) 
            throw new ApplicationException("Missing Binary Path");

        if (SplashPathx2 is null && SplashPath is null)
            throw new ApplicationException("Required at least one splash screen");
        // At least one of them is assigned - it's okay I guess
        // TODO make auto resize as: SplashPathx2 => SplashPath
        SplashPath ??= SplashPathx2;
        SplashPathx2 ??= SplashPath;
        if (!File.Exists(SplashPath) || !File.Exists(SplashPathx2))
            throw new ApplicationException("Splash screen files doesn't exist!");

        if (!ImageUtils.FileIsImage(SplashPath) || !ImageUtils.FileIsImage(SplashPathx2))
            throw new ApplicationException("File is not an image!");
    }

    // TODO: Mess, refactoring is needed (to discuss)
    private void PatchJarWithNewSplashes(ZipFile file)
    {
        foreach (ZipEntry entry in file)
        {
            // We don't care about directories and any file that not splash.png related
            var skip = !(entry.Name.Contains("splash") && entry.Name.EndsWith(".png"));
            if (skip) continue;

            Console.WriteLine($"Patching: {entry.Name}");

            if (entry.Name.Contains("x2"))
            {
                using var splashImagex2 = Image.Load(SplashPathx2);
                splashImagex2.Mutate(x => x
                    .Resize(1280, 800));
                ImageUtils.SaveImageToZipAs(splashImagex2, file, new PngEncoder(), entry.Name);
                continue;
            }

            using var splashImage = Image.Load(SplashPath);
            splashImage.Mutate(x => x
                .Resize(640,400));
            ImageUtils.SaveImageToZipAs(splashImage, file, new PngEncoder(), entry.Name);
        }
    }

    private void BackupOldApp()
    {
        File.Copy(BinaryPath, OldFilePath, true);
        Console.WriteLine($"Old app.jar saved at {OldFilePath}");
    }

    private void RevertOldAppFile()
    {
        Console.WriteLine($"Reverting app.jar");
        File.Move(OldFilePath, BinaryPath, true);
    }
    
    public void PatchJar()
    {
        EnsurePatcherValid();
        // TODO flag to enforce old app.jar rewriting
        BackupOldApp();
        try
        {
            using var jarFile = new ZipFile(BinaryPath);
            if (jarFile.Count is 0) throw new ApplicationException("Jar is empty!");
            jarFile.BeginUpdate();
            PatchJarWithNewSplashes(jarFile);
            jarFile.CommitUpdate();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            RevertOldAppFile();
            throw;
        }
    }
}
