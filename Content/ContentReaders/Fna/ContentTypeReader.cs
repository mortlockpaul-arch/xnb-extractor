using System;
using System.IO;

namespace XnbExtractor.Content.ContentReaders.Fna
{
	public abstract class ContentTypeReader
	{

		public virtual bool CanDeserializeIntoExistingObject
		{
			get
			{
				return false;
			}
		}

		public Type TargetType
		{
			get
			{
				return this.targetType;
			}
		}

		public virtual int TypeVersion
		{
			// The default version (unless overridden) is zero
			get
			{
				return 0;
			}
		}



		private Type targetType;



		protected ContentTypeReader(Type targetType)
		{
			this.targetType = targetType;
		}



		protected internal virtual void Initialize(ContentTypeReaderManager manager)
		{
			// Do nothing. Are we supposed to add ourselves to the manager?
		}



		protected internal abstract object Read(ContentReader input, object existingInstance);

	}

	public abstract class ContentTypeReader<T> : ContentTypeReader
	{

		protected ContentTypeReader() : base(typeof(T))
		{
		}



		/// <summary>
		/// Reads an object from the input stream.
		/// </summary>
		/// <param name="input">The input stream.</param>
		/// <param name="existingInstance">
		/// Existing instance of an object to receive the data, or null if a new object
		/// instance should be created.
		/// </param>
		protected internal override object Read(ContentReader input, object existingInstance)
		{
			if (existingInstance == null)
			{
				return this.Read(input, default(T));
			}
			else
			{
				return this.Read(input, (T) existingInstance);
			}
		}

		protected internal abstract T Read(ContentReader input, T existingInstance);

	}
}

