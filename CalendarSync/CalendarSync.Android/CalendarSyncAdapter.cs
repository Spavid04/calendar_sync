using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Accounts;

namespace CalendarSync.Droid
{
    [MetaData("com.dpop.calendarsync", Resource = "@xml/syncadapter")]
    public class CalendarSyncAdapter : AbstractThreadedSyncAdapter
    {
        public CalendarSyncAdapter(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public CalendarSyncAdapter(Context context, bool autoInitialize) : base(context, autoInitialize)
        {
        }

        public CalendarSyncAdapter(Context context, bool autoInitialize, bool allowParallelSyncs) : base(context, autoInitialize, allowParallelSyncs)
        {
        }

        public override void OnPerformSync(Account account, Bundle extras, string authority, ContentProviderClient provider,
            SyncResult syncResult)
        {
            throw new NotImplementedException();
        }
    }
}