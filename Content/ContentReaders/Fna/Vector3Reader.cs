
using Microsoft.Xna.Framework;

namespace XnbExtractor.Content.ContentReaders.Fna
{
	internal class Vector3Reader : ContentTypeReader<Vector3>
    {

		internal Vector3Reader()
		{
		}



		protected internal override Vector3 Read(
			ContentReader input,
			Vector3 existingInstance
		) {
			return input.ReadVector3();
		}

	}
}

