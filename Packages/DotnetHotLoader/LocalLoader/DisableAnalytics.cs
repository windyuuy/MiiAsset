using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableAnalytics : MonoBehaviour
{
	private void Awake()
	{
		UnityEngine.Analytics.Analytics.enabled = false;
		UnityEngine.Analytics.Analytics.deviceStatsEnabled = false;
		UnityEngine.Analytics.Analytics.initializeOnStartup = false;
		UnityEngine.Analytics.Analytics.limitUserTracking = false;
		UnityEngine.Analytics.PerformanceReporting.enabled = false;
	}
}
