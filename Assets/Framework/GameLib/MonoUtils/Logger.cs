
using System.Collections.Generic;
using Lang.TypeHelper;
using Console = Game.Diagnostics.Console;

namespace Lang.Loggers
{
	using boolean = System.Boolean;
	using Error = System.Exception;
	using JSON = Lang.Json.JSON;
	using Object = System.Object;
	using StringBuilder = System.Text.StringBuilder;

	/**
     * 日志参数
     */
	public interface ILogParam
	{
		boolean Time { get; set; }
		string[] Tags { get; set; }
	}

	public class LogParam : ILogParam
	{
		private bool _time;
		public bool Time
		{
			get => _time;
			set => _time = value;
		}
		private string[] _tags;
		public string[] Tags
		{
			get => _tags;
			set => _tags = value;
		}
	}

	public class Logger
	{

		private static boolean _enablePlainLog = false;
		/**
         * 是否启用平铺日志
         * - 如果启用平铺日志, 将会直接序列化日志对象, 转换为字符串打印出来
         */
		public static boolean EnablePlainLog
		{
			get
			{
				return Logger._enablePlainLog;
			}
			set
			{
				Logger._enablePlainLog = value;
			}
		}

		/**
		 * 将对象转换为平铺日志
		 * - 如果启用平铺日志, 将会直接序列化日志对象, 转换为字符串打印出来
		 */
		public static List<string> ToPlainLog(object[] args)
		{
			var plainTexts = new List<string>();

			foreach (var info in args)
			{
				var ret = "";

				if (info is Error)
				{
					var err = info as Error;
					ret = $"Error content: { JSON.Stringify(err)}\n{ err.StackTrace}";

				}
				else if (info is Object)
				{
					ret = JSON.Stringify(info);

				}
				else
				{
					if (info is string)
					{
						ret = info as string;
					}
					else
					{
						ret = info.ToString();
					}

				}
				plainTexts.Add(ret);


			}

			return plainTexts;
		}

		private static Logger _instance;
		/**
         * 可选使用的单例
         */
		public static Logger Inst
		{
			get
			{
				if (Logger._instance == null)
				{
					Logger._instance = new Logger();
				}
				return Logger._instance;
			}
		}

		/**
		 * 是否打印时间戳
		 */
		protected boolean Time;
		/**
         * 日志标签
         */
		protected List<string> Tags;
		/**
         * 日志选项内容是否需要更新
         */
		protected boolean Dirty = true;


		public Logger(ILogParam x = null)
		{
			if (x == null)
			{
				x = new LogParam();
			}
			this.SetLogOptions(x);
		}

		/**
		 * 尾部追加标签
		 * @param tag 
		 * @returns 
		 */
		public Logger AppendTag(string tag)
		{
			if (this.Tags != null)
			{
				this.Tags.Add(tag);
			}
			else
			{
				// this.tags = new string[] { tag };
				this.Tags = new List<string>(1);
				this.Tags.Add(tag);
			}
			this.Dirty = this.Dirty || tag != null;

			return this;
		}

		/**
		 * 尾部追加标签列表
		 * @param tags 
		 * @returns 
		 */
		public Logger AppendTags(string[] tags)
		{
			foreach (var tag in tags)
			{
				this.AppendTag(tag);
			}
			this.Dirty = this.Dirty || tags.Length > 0;
			return this;
		}

		/**
		 * 设置日志选项
		 * @param param0 
		 * @returns 
		 */
		public Logger SetLogOptions(ILogParam p = null)
		{
			var time = p.Time;
			var tags = p.Tags;

			this.Time = time;

			if (tags != null)
			{
				this.Tags = tags.Clone() as List<string>;

			}
			this.Dirty = true;

			return this;

		}

		/**
		 * 缓存的日志标签戳
		 */
		protected string CachedTagsStamp;
		/**
         * 获取日志标签戳
         * @returns 
         */
		protected string GetTagsStamp()
		{
			if (!this.Dirty)
			{
				return this.CachedTagsStamp;
			}

			string tag;
			if (this.Tags != null)
			{
				tag = $"[{ string.Join("][", this.Tags)}]";
			}
			else
			{
				tag = "";



			}

			if (this.Time)
			{
				tag = tag + $"[t/{ System.DateTime.Now}]";
			}

			this.CachedTagsStamp = tag;

			this.Dirty = false;


			return tag;

		}

		/**
		 * log通道打印日志，并储至日志文件
		 * @param args 
		 */
		public void Log(params object[] args)
		{
			// if (this.tags) {
			//     args = this.tags.concat(args)
			// }
			// if (this.time) {
			//     args.push(new Date().getTime())
			// }

			Console.Log(ConvParaToString(args));
		}

		private StringBuilder ConvParaToString(object[] args)
		{
			var stringBuilder = new StringBuilder(" -", 2 + args.Length).Append(this.GetTagsStamp());
			foreach (var arg in args)
			{
				stringBuilder.Append(" ");
				if (arg.GetType().IsPrimitive || arg is string)
				{
					stringBuilder.Append(arg);
				}
				else
				{
					stringBuilder.Append(JSON.Stringify(arg));
				}
			}
			return stringBuilder;
		}

		/**
		 * 将消息打印到控制台，不存储至日志文件
		 */
		public void Debug(params object[] args)
		{
			// if (this.tags) {
			//     args = this.tags.concat(args)
			// }
			// if (this.time) {
			//     args.push(new Date().getTime())
			// }
			Console.Log(ConvParaToString(args));
		}

		/**
		 * 将消息打印到控制台，不存储至日志文件
		 */
		public void Info(params object[] args)
		{
			// if (this.tags) {
			//     args = this.tags.concat(args)
			// }
			// if (this.time) {
			//     args.push(new Date().getTime())
			// }
			Console.Log(ConvParaToString(args));
		}

		/**
		 * 将消息打印到控制台，并储至日志文件
		 */
		public void Warn(params object[] args)
		{
			// if (this.tags) {
			//     args = this.tags.concat(args)
			// }
			// if (this.time) {
			//     args.push(new Date().getTime())
			// }
			Console.LogWarning(ConvParaToString(args));
		}

		/**
		 * 将消息打印到控制台，并储至日志文件
		 */
		public void Error(params object[] args)
		{
			// if (this.tags) {
			//     args = this.tags.concat(args)
			// }
			// if (this.time) {
			//     args.push(new Date().getTime())
			// }
			Console.LogError(ConvParaToString(args));
			foreach (var p in args)
			{
				if (p is Error)
				{
					var e = p as Error;
					Console.Log(e.StackTrace);
				}
			}
			Console.Log(">>>error");

			Console.Log(new Error().StackTrace);

		}

		/**
		 * 从目标覆盖日志选项到自身
		 * @param source 
		 */
		public Logger MergeFrom(Logger source)
		{
			this.Time = source.Time;



			if (source.Tags != null)
			{
				if (this.Tags != null)
				{
					for (var i = 0; i < source.Tags.Count; i++)
					{
						this.Tags[i] = source.Tags[i];
					}
				}
				else
				{
					this.Tags = source.Tags.Clone();
				}
			}
			else
			{
				if (this.Tags != null)
				{
					this.Tags.Clear();
				}
			}
			this.Dirty = source.Dirty;
			this.CachedTagsStamp = source.CachedTagsStamp;
			return this;
		}

		/**
		 * 克隆自己
		 * @returns 
		 */
		public Logger Clone()
		{
			var log = new Logger();
			log.MergeFrom(this);
			return log;
		}

	}

}