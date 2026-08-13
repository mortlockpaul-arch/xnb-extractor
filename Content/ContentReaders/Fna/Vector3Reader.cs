
namespace XnbExtractor.Content.ContentReaders.Fna
{
	internal class Vector3Reader : IXnbTypeReader
    {

		internal Vector3Reader()
		{
		}



		protected internal override object Read(
			XnbContentReader input,
			object existingInstance
		) {
			return input.ReadVector3();
		}

	}
}

