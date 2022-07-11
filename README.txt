This is a set of utilities that exports Outlook's (the Windows app; not the service/website/whatever else) calendar in a friendly ical file, and allows its use in any compatible calendar application.
It's designed to allow manually or automatically syncing said Outlook calendar to, for example, your phone's calendar, without logging in to the Microsoft account (or any account, for that matter).
It's intended to be used when corporate "security" policies don't allow external devices to log into your Microsoft account for genuine reasons, but then don't provide any convenient workaround for accessing non-critical data, such as the calendar (barring any links that shouldn't be public).

It's split into 3 parts:
* a Windows application that fetches calendar events from a running Outlook instance, and then exports it wherever in ical format
* a web server that allows storing and retrieving the ical files in a simple and secure-enough way
* [unfinished] an Android/iOS app that exposes a calendar sync service, with the ical file as the backing store
All 3 components are optional and can probably be mixed together with any other such utility.

Useful links:
* https://github.com/insanum/gcalcli - together with the Outlook exporter alone, it can be used to push calendar events to a Google account
