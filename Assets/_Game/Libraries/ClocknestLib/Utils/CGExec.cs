/// <summary>
/// https://gist.github.com/karljj1/9c6cce803096b5cd4511cf0819ff517b
/// </summary>

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClocknestGames.Game.Core
{
	public static class CGExec
	{
		private class HiddenMonobehaviour : MonoBehaviour { }

		private static MonoBehaviour mono;

		private static Dictionary<string, Coroutine> coroutines = new Dictionary<string, Coroutine>();

		//TO SUPPORT DOMAIN RELOADING WHEN DISABLED
		[RuntimeInitializeOnLoadMethod]
		static void Init()
		{
			GameObject obj = new("CGExec");
			obj.hideFlags = HideFlags.HideInHierarchy;
			UnityEngine.Object.DontDestroyOnLoad(obj);
			mono = obj.AddComponent<HiddenMonobehaviour>();
		}

		/// <summary>Delays <b>action</b> by <b>delay (in seconds)</b></summary>
		/// <param name="delay">Duration in seconds</param>
		public static Coroutine Run(float delay, Action action)
		{
			return mono.StartCoroutine(InternalRun(delay, action));
		}

		/// <summary>Method will delay the action by the delay given (in seconds). If another coroutine with same name is running it will stop that one</summary>
		/// <param name="delay">Duration in seconds</param>
		public static void Run(float delay, Action action, string name)
		{
			if (coroutines.ContainsKey(name))
				mono.StopCoroutine(coroutines[name]);

			coroutines[name] = mono.StartCoroutine(InternalRun(delay, action));
		}

		public static Coroutine Run<T>(float delay, Action<T> action, T parameter)
		{
			return mono.StartCoroutine(InternalRun(delay, action, parameter));
		}

		public static Coroutine Run<T, U>(float delay, Action<T, U> action, T param1, U param2)
		{
			return mono.StartCoroutine(InternalRun(delay, action, param1, param2));
		}

		public static Coroutine Run<T, U, X>(float delay, Action<T, U, X> action, T param1, U param2, X param3)
		{
			return mono.StartCoroutine(InternalRun(delay, action, param1, param2, param3));
		}

		private static IEnumerator InternalRun(float delay, Action action)
		{
			yield return new WaitForSeconds(delay);
			action.Invoke();
		}

		private static IEnumerator InternalRun<T>(float delay, Action<T> action, T parameter)
		{
			yield return new WaitForSeconds(delay);
			action.Invoke(parameter);
		}

		private static IEnumerator InternalRun<T, U>(float delay, Action<T, U> action, T parameter1, U parameter2)
		{
			yield return new WaitForSeconds(delay);
			action.Invoke(parameter1, parameter2);
		}

		private static IEnumerator InternalRun<T, U, X>(float delay, Action<T, U, X> action, T parameter1, U parameter2, X parameter3)
		{
			yield return new WaitForSeconds(delay);
			action.Invoke(parameter1, parameter2, parameter3);
		}

		public static Coroutine OnNextFrame(Action action)
		{
			return mono.StartCoroutine(InternalNextFrame(action));
		}

		private static IEnumerator InternalNextFrame(Action action)
		{
			yield return 0;
			action.Invoke();
		}

		public static void IterateOverTime(float duration, Action action, int iterationsPerSecond = 60)
		{
			mono.StartCoroutine(InternalIterateOverTime(duration, action, iterationsPerSecond));
		}

		private static IEnumerator InternalIterateOverTime(float duration, Action action, int iterationsPerSecond)
		{
			WaitForSeconds wait = new WaitForSeconds(1f / iterationsPerSecond);
			float startTime = Time.time;
			for (float timePassed = 0; timePassed < duration; timePassed = Time.time - startTime)
			{
				action.Invoke();
				yield return wait;
			}
		}

		public static TResult RunInMode<TResult>(Func<TResult> playMode, Func<TResult> editorMode)
		{
#if UNITY_EDITOR
			return Application.isPlaying ? playMode.Invoke() : editorMode.Invoke();
#endif
			return playMode.Invoke();
		}

		public static void RunInMode(Action playMode, Action editorMode)
		{
#if UNITY_EDITOR
			if (Application.isPlaying)
				playMode.Invoke();
			else
				editorMode.Invoke();

			return;
#endif
			playMode.Invoke();
		}
	}
}