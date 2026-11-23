package org.archeion.companion.providers

import android.content.ContentValues
import android.content.Context
import android.provider.CalendarContract
import java.io.ByteArrayOutputStream
import java.io.InputStream
import java.io.OutputStreamWriter
import java.nio.charset.StandardCharsets
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import java.util.TimeZone

data class CalendarEntry(
    val id: Long,
    val title: String,
    val description: String?,
    val eventLocation: String?,
    val dtStart: Long,
    val dtEnd: Long,
    val allDay: Boolean,
    val rRule: String?
)

class CalendarIcsExporter(private val context: Context) {

    private val icsDateFormat = SimpleDateFormat("yyyyMMdd'T'HHmmss'Z'", Locale.US).apply {
        timeZone = TimeZone.getTimeZone("UTC")
    }

    fun readAllEvents(): List<CalendarEntry> {
        val events = mutableListOf<CalendarEntry>()
        val cr = context.contentResolver

        val projection = arrayOf(
            CalendarContract.Events._ID,
            CalendarContract.Events.TITLE,
            CalendarContract.Events.DESCRIPTION,
            CalendarContract.Events.EVENT_LOCATION,
            CalendarContract.Events.DTSTART,
            CalendarContract.Events.DTEND,
            CalendarContract.Events.ALL_DAY,
            CalendarContract.Events.RRULE
        )

        val cursor = cr.query(
            CalendarContract.Events.CONTENT_URI,
            projection,
            null,
            null,
            "${CalendarContract.Events.DTSTART} ASC"
        ) ?: return emptyList()

        cursor.use { c ->
            val idCol = c.getColumnIndex(CalendarContract.Events._ID)
            val titleCol = c.getColumnIndex(CalendarContract.Events.TITLE)
            val descCol = c.getColumnIndex(CalendarContract.Events.DESCRIPTION)
            val locCol = c.getColumnIndex(CalendarContract.Events.EVENT_LOCATION)
            val startCol = c.getColumnIndex(CalendarContract.Events.DTSTART)
            val endCol = c.getColumnIndex(CalendarContract.Events.DTEND)
            val allDayCol = c.getColumnIndex(CalendarContract.Events.ALL_DAY)
            val rruleCol = c.getColumnIndex(CalendarContract.Events.RRULE)

            while (c.moveToNext()) {
                events.add(
                    CalendarEntry(
                        id = c.getLong(idCol),
                        title = c.getString(titleCol) ?: "Untitled Event",
                        description = c.getString(descCol),
                        eventLocation = c.getString(locCol),
                        dtStart = c.getLong(startCol),
                        dtEnd = c.getLong(endCol),
                        allDay = c.getInt(allDayCol) == 1,
                        rRule = c.getString(rruleCol)
                    )
                )
            }
        }

        return events
    }

    fun exportToIcs(): ByteArray {
        val events = readAllEvents()
        val bos = ByteArrayOutputStream()
        val writer = OutputStreamWriter(bos, StandardCharsets.UTF_8)

        writer.write("BEGIN:VCALENDAR\r\n")
        writer.write("VERSION:2.0\r\n")
        writer.write("PRODID:-//Archeion//Archeion Companion 1.0//EN\r\n")
        writer.write("CALSCALE:GREGORIAN\r\n")

        for (ev in events) {
            writer.write("BEGIN:VEVENT\r\n")
            writer.write("UID:archeion-${ev.id}@companion\r\n")
            writer.write("DTSTAMP:${icsDateFormat.format(Date())}\r\n")
            writer.write("DTSTART:${icsDateFormat.format(Date(ev.dtStart))}\r\n")
            if (ev.dtEnd > ev.dtStart) {
                writer.write("DTEND:${icsDateFormat.format(Date(ev.dtEnd))}\r\n")
            }
            writer.write("SUMMARY:${escapeIcs(ev.title)}\r\n")
            if (!ev.description.isNullOrBlank()) {
                writer.write("DESCRIPTION:${escapeIcs(ev.description)}\r\n")
            }
            if (!ev.eventLocation.isNullOrBlank()) {
                writer.write("LOCATION:${escapeIcs(ev.eventLocation)}\r\n")
            }
            if (!ev.rRule.isNullOrBlank()) {
                writer.write("RRULE:${ev.rRule}\r\n")
            }
            writer.write("END:VEVENT\r\n")
        }

        writer.write("END:VCALENDAR\r\n")
        writer.flush()
        return bos.toByteArray()
    }

    fun importIcs(inputStream: InputStream, targetCalendarId: Long = 1): Int {
        val lines = inputStream.bufferedReader(StandardCharsets.UTF_8).readLines()
        var imported = 0
        var title: String? = null
        var desc: String? = null
        var loc: String? = null
        var dtStart: Long = 0
        var dtEnd: Long = 0

        for (line in lines) {
            val trimmed = line.trim()
            when {
                trimmed == "BEGIN:VEVENT" -> {
                    title = null
                    desc = null
                    loc = null
                    dtStart = 0
                    dtEnd = 0
                }
                trimmed.startsWith("SUMMARY:") -> title = unescapeIcs(trimmed.removePrefix("SUMMARY:"))
                trimmed.startsWith("DESCRIPTION:") -> desc = unescapeIcs(trimmed.removePrefix("DESCRIPTION:"))
                trimmed.startsWith("LOCATION:") -> loc = unescapeIcs(trimmed.removePrefix("LOCATION:"))
                trimmed.startsWith("DTSTART:") -> {
                    val raw = trimmed.removePrefix("DTSTART:")
                    try { dtStart = icsDateFormat.parse(raw)?.time ?: 0 } catch (_: Exception) {}
                }
                trimmed.startsWith("DTEND:") -> {
                    val raw = trimmed.removePrefix("DTEND:")
                    try { dtEnd = icsDateFormat.parse(raw)?.time ?: 0 } catch (_: Exception) {}
                }
                trimmed == "END:VEVENT" -> {
                    if (title != null && dtStart > 0) {
                        val values = ContentValues().apply {
                            put(CalendarContract.Events.CALENDAR_ID, targetCalendarId)
                            put(CalendarContract.Events.TITLE, title)
                            put(CalendarContract.Events.DESCRIPTION, desc)
                            put(CalendarContract.Events.EVENT_LOCATION, loc)
                            put(CalendarContract.Events.DTSTART, dtStart)
                            put(CalendarContract.Events.DTEND, if (dtEnd > dtStart) dtEnd else dtStart + 3600000L)
                            put(CalendarContract.Events.EVENT_TIMEZONE, TimeZone.getDefault().id)
                        }
                        try {
                            context.contentResolver.insert(CalendarContract.Events.CONTENT_URI, values)
                            imported++
                        } catch (_: Exception) {}
                    }
                }
            }
        }
        return imported
    }

    private fun escapeIcs(str: String): String =
        str.replace("\\", "\\\\").replace(";", "\\;").replace(",", "\\,").replace("\n", "\\n")

    private fun unescapeIcs(str: String): String =
        str.replace("\\n", "\n").replace("\\,", ",").replace("\\;", ";").replace("\\\\", "\\")
}
