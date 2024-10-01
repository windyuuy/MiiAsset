
namespace fsync
{
	using TTimeStamp = System.Int64;

	/// <summary>
	/// 网络时间管理
	/// - 使用前需要设置 setStartTime 和 updateTime
	/// </summary>
	public class TTimer
	{

		/// <summary>
		/// 外界实际当前时间点
		/// </summary>
		protected TTimeStamp _curTimeRecord = 0;

		/// <summary>
		/// 游戏内部当前时间点
		/// - 从一局开始
		/// </summary>
		protected TTimeStamp _curTime = 0;

		/// <summary>
		/// 游戏当前帧开始时间
		/// - 从一局开始
		/// </summary>
		protected TTimeStamp _lastTime = 0;

		/// <summary>
		/// 当前帧间间隔
		/// </summary>
		protected TTimeStamp _deltaTime = 0;

		/// <summary>
		/// 最大帧间隔,用于提升断点调试体验
		/// </summary>
		protected TTimeStamp _maxDeltaTime = TTimeStamp.MaxValue;

		/// <summary>
		/// 游戏开始时间点
		/// </summary>
		protected TTimeStamp _startTime = 0;

		/// <summary>
		/// 获取当前游戏时间戳
		/// - 和 getGameTime() 的区别在于, getGameTime 的起始时间点为 0, getTime 的起始时间点和游戏开始时的 Date.now() 基本一致
		/// </summary>
		/// <returns></returns>
		public TTimeStamp getTime()
		{
			return this._curTimeRecord;
		}


		/// <summary>
		/// 用于逐步更新游戏时间点进度
		/// </summary>
		/// <param name="time"></param>
		public void updateTime(TTimeStamp time)
		{
			var dt = time - this._curTimeRecord;
			this._curTimeRecord = time;
			// this._deltaTime = Math.Min((float)dt, (float)this._maxDeltaTime);
			this._deltaTime = dt <= this._maxDeltaTime ? dt : this._maxDeltaTime;
			this._lastTime = this._curTime;
			this._curTime = this._curTime + this._deltaTime;
		}

		public void updateTime(ulong time)
		{
			this.updateTime((long)time);
		}

		/// <summary>
		/// 重设游戏开始时间点
		/// </summary>
		/// <param name="time"></param>
		public void setStartTime(TTimeStamp time)
		{
			this._startTime = time;
			this._curTimeRecord = time;
			this._curTime = 0;
			this._lastTime = 0;
			this._deltaTime = 0;
			this._frameCount = 0;
		}
		public void setStartTime(ulong time)
		{
			this.setStartTime((long)time);
		}

		/// <summary>
		/// 游戏已进行时长
		/// </summary>
		/// <returns></returns>
		public TTimeStamp getGameTime()
		{
			return this._curTime;
		}
		/// <summary>
		/// 当前帧开始时间
		/// - 为游戏时长计时
		/// - 相当于 Time.time
		/// </summary>
		/// <returns></returns>
		public float time => (float)(this.getGameTime()) / 1000;

		public void setTime(TTimeStamp time)
		{
			this._curTime = time;
			this._deltaTime = 0;
			this.setStartTime(time);
		}

		public float deltaTime
		{
			get
			{
				return (float)(this._deltaTime) / 1000;
			}
		}

		public float timeSinceLevelLoad => (float)(this.getGameTime()) / 1000;

		public int timeScale
		{
			get
			{
				throw new System.NotImplementedException("联网游戏暂不支持暂停游戏");
			}
		}

		public void setMaxDeltaTime(TTimeStamp dt)
		{
			this._maxDeltaTime = dt;
		}

		/// <summary>
		/// 逻辑帧率
		/// </summary>
		public long frameCount => this._frameCount - _startFrameCount;
		protected long _startFrameCount = 0;
		protected long _frameCount = 0;

		public void setStartFrameCount(long frameCount)
		{
			this._startFrameCount = frameCount;
		}

		/// <summary>
		/// 刷新逻辑帧率
		/// </summary>
		public void updateFrameCount(long frameCount)
		{
			this._frameCount = frameCount;
		}
	}
}
