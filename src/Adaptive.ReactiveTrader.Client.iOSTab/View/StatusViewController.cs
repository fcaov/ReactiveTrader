﻿
using System;
using System.Drawing;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Adaptive.ReactiveTrader.Client.Domain;
using Adaptive.ReactiveTrader.Client.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;

namespace Adaptive.ReactiveTrader.Client.iOSTab.View
{
	public partial class StatusViewController : UIViewController
	{
		private readonly IReactiveTrader _reactiveTrader;
		private readonly IConcurrencyService _concurrencyService;
		private readonly CompositeDisposable _disposables = new CompositeDisposable();

		ConnectionInfo _lastConnectionInfo;

		public StatusViewController (IReactiveTrader reactiveTrader, IConcurrencyService concurrencyService) : base ("StatusViewController", null)
		{
			_reactiveTrader = reactiveTrader;
			_concurrencyService = concurrencyService;

			Title = "Status";
			TabBarItem.Image = UIImage.FromBundle ("adaptive");

			_disposables.Add (
				_reactiveTrader.ConnectionStatusStream
					.SubscribeOn (_concurrencyService.TaskPool)
					.ObserveOn (_concurrencyService.Dispatcher)
					.Subscribe (OnStatusChange));

			_disposables.Add (
				Observable.Interval (TimeSpan.FromSeconds(1), _concurrencyService.TaskPool)
					.ObserveOn (_concurrencyService.Dispatcher)
					.Subscribe(_ => OnTimer()));

		}

		protected override void Dispose (bool disposing)
		{	
			base.Dispose (disposing);

			if (disposing) {
				_disposables.Dispose();
			}
		}

		public override void DidReceiveMemoryWarning ()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();
			
			// Release any cached data, images, etc that aren't in use.
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			// Perform any additional setup after loading the view, typically from a nib.

			if (_lastConnectionInfo != null) {
				OnStatusChange (_lastConnectionInfo);
				_lastConnectionInfo = null;
			}
		}

		private void OnStatusChange(ConnectionInfo connectionInfo) {
			if (this.IsViewLoaded) {
				this.ConnectionUrl.Text = connectionInfo.Server;
				this.ConnectionStatus.Text = connectionInfo.ConnectionStatus.ToString ();
			} else {
				_lastConnectionInfo = connectionInfo;
			}
		}

		private void OnTimer() {
			var statistics = _reactiveTrader.PriceLatencyRecorder.CalculateAndReset ();

			if (this.IsViewLoaded) {
				this.ServerUpdateRate.Text = string.Format ("{0} / sec", statistics.ReceivedCount);
				this.UIUpdateRate.Text = string.Format ("{0} / sec", statistics.RenderedCount);
				this.UILatency.Text = string.Format ("{0} ms", statistics.RenderedCount);
			}
		}
	}
}

