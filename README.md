# webanide
Light weight library to build HTML5 desktop apps in dotnet. 
This requires a chrome/chromium based browser for the UI layer instead of bundling chrome like in Electron.

```
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var ui = UI.New("https://loable.tech", "", 800, 700);
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
    }
```
