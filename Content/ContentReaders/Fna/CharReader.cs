/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2024 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */

/* Derived from code by the Mono.Xna Team (Copyright 2006).
 * Released under the MIT License. See monoxna.LICENSE for details.
 */

namespace XnbExtractor.Content.ContentReaders.Fna
{
	internal class CharReader : ContentTypeReader<char>
	{

		internal CharReader()
		{
		}



		protected internal override char Read(
			ContentReader input,
			char existingInstance
		) {
			return input.ReadChar();
		}

	}
}

