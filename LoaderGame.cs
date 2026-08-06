using System;

namespace XnbExtractor
{
    internal class LoaderGame: IDisposable
    {
        private string inputFile;

        public LoaderGame(string inputFile)
        {
            this.inputFile = inputFile;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}