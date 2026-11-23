package org.archeion.companion.providers

import android.content.ContentProviderOperation
import android.content.Context
import android.provider.ContactsContract
import java.io.ByteArrayOutputStream
import java.io.InputStream
import java.io.OutputStreamWriter
import java.nio.charset.StandardCharsets

data class ContactEntry(
    val id: String,
    val displayName: String,
    val phoneNumbers: List<String> = emptyList(),
    val emailAddresses: List<String> = emptyList(),
    val postalAddresses: List<String> = emptyList(),
    val notes: String? = null
)

class ContactsVcfExporter(private val context: Context) {

    fun readAllContacts(): List<ContactEntry> {
        val contactsMap = mutableMapOf<String, ContactEntry>()
        val cr = context.contentResolver

        val cursor = cr.query(
            ContactsContract.Contacts.CONTENT_URI,
            arrayOf(ContactsContract.Contacts._ID, ContactsContract.Contacts.DISPLAY_NAME_PRIMARY),
            null,
            null,
            null
        ) ?: return emptyList()

        cursor.use { c ->
            val idCol = c.getColumnIndex(ContactsContract.Contacts._ID)
            val nameCol = c.getColumnIndex(ContactsContract.Contacts.DISPLAY_NAME_PRIMARY)
            while (c.moveToNext()) {
                val id = c.getString(idCol)
                val name = c.getString(nameCol) ?: "Unnamed"
                contactsMap[id] = ContactEntry(id = id, displayName = name)
            }
        }

        // Fetch phones
        val phoneCursor = cr.query(
            ContactsContract.CommonDataKinds.Phone.CONTENT_URI,
            arrayOf(ContactsContract.CommonDataKinds.Phone.CONTACT_ID, ContactsContract.CommonDataKinds.Phone.NUMBER),
            null,
            null,
            null
        )
        phoneCursor?.use { c ->
            val idCol = c.getColumnIndex(ContactsContract.CommonDataKinds.Phone.CONTACT_ID)
            val numCol = c.getColumnIndex(ContactsContract.CommonDataKinds.Phone.NUMBER)
            while (c.moveToNext()) {
                val id = c.getString(idCol)
                val num = c.getString(numCol)
                val existing = contactsMap[id]
                if (existing != null && num != null) {
                    contactsMap[id] = existing.copy(phoneNumbers = existing.phoneNumbers + num)
                }
            }
        }

        // Fetch emails
        val emailCursor = cr.query(
            ContactsContract.CommonDataKinds.Email.CONTENT_URI,
            arrayOf(ContactsContract.CommonDataKinds.Email.CONTACT_ID, ContactsContract.CommonDataKinds.Email.ADDRESS),
            null,
            null,
            null
        )
        emailCursor?.use { c ->
            val idCol = c.getColumnIndex(ContactsContract.CommonDataKinds.Email.CONTACT_ID)
            val emailCol = c.getColumnIndex(ContactsContract.CommonDataKinds.Email.ADDRESS)
            while (c.moveToNext()) {
                val id = c.getString(idCol)
                val email = c.getString(emailCol)
                val existing = contactsMap[id]
                if (existing != null && email != null) {
                    contactsMap[id] = existing.copy(emailAddresses = existing.emailAddresses + email)
                }
            }
        }

        return contactsMap.values.toList()
    }

    fun exportToVcf(): ByteArray {
        val contacts = readAllContacts()
        val bos = ByteArrayOutputStream()
        val writer = OutputStreamWriter(bos, StandardCharsets.UTF_8)

        for (contact in contacts) {
            writer.write("BEGIN:VCARD\r\n")
            writer.write("VERSION:3.0\r\n")
            writer.write("FN:${escapeVCard(contact.displayName)}\r\n")
            writer.write("N:;${escapeVCard(contact.displayName)};;;\r\n")

            for (phone in contact.phoneNumbers) {
                writer.write("TEL;TYPE=CELL:${phone.trim()}\r\n")
            }
            for (email in contact.emailAddresses) {
                writer.write("EMAIL;TYPE=INTERNET:${email.trim()}\r\n")
            }
            if (!contact.notes.isNullOrBlank()) {
                writer.write("NOTE:${escapeVCard(contact.notes)}\r\n")
            }
            writer.write("END:VCARD\r\n")
        }

        writer.flush()
        return bos.toByteArray()
    }

    fun importVcf(inputStream: InputStream): Int {
        val lines = inputStream.bufferedReader(StandardCharsets.UTF_8).readLines()
        var importedCount = 0
        var currentName: String? = null
        var currentPhones = mutableListOf<String>()
        var currentEmails = mutableListOf<String>()

        for (line in lines) {
            val trimmed = line.trim()
            when {
                trimmed == "BEGIN:VCARD" -> {
                    currentName = null
                    currentPhones.clear()
                    currentEmails.clear()
                }
                trimmed.startsWith("FN:") -> {
                    currentName = unescapeVCard(trimmed.removePrefix("FN:"))
                }
                trimmed.startsWith("TEL") -> {
                    val idx = trimmed.indexOf(':')
                    if (idx != -1) currentPhones.add(trimmed.substring(idx + 1))
                }
                trimmed.startsWith("EMAIL") -> {
                    val idx = trimmed.indexOf(':')
                    if (idx != -1) currentEmails.add(trimmed.substring(idx + 1))
                }
                trimmed == "END:VCARD" -> {
                    if (currentName != null) {
                        saveRawContact(currentName, currentPhones, currentEmails)
                        importedCount++
                    }
                }
            }
        }
        return importedCount
    }

    private fun saveRawContact(name: String, phones: List<String>, emails: List<String>) {
        val ops = ArrayList<ContentProviderOperation>()

        val rawContactInsertIndex = ops.size
        ops.add(
            ContentProviderOperation.newInsert(ContactsContract.RawContacts.CONTENT_URI)
                .withValue(ContactsContract.RawContacts.ACCOUNT_TYPE, null)
                .withValue(ContactsContract.RawContacts.ACCOUNT_NAME, null)
                .build()
        )

        ops.add(
            ContentProviderOperation.newInsert(ContactsContract.Data.CONTENT_URI)
                .withValueBackReference(ContactsContract.Data.RAW_CONTACT_ID, rawContactInsertIndex)
                .withValue(ContactsContract.Data.MIMETYPE, ContactsContract.CommonDataKinds.StructuredName.CONTENT_ITEM_TYPE)
                .withValue(ContactsContract.CommonDataKinds.StructuredName.DISPLAY_NAME, name)
                .build()
        )

        for (phone in phones) {
            ops.add(
                ContentProviderOperation.newInsert(ContactsContract.Data.CONTENT_URI)
                    .withValueBackReference(ContactsContract.Data.RAW_CONTACT_ID, rawContactInsertIndex)
                    .withValue(ContactsContract.Data.MIMETYPE, ContactsContract.CommonDataKinds.Phone.CONTENT_ITEM_TYPE)
                    .withValue(ContactsContract.CommonDataKinds.Phone.NUMBER, phone)
                    .withValue(ContactsContract.CommonDataKinds.Phone.TYPE, ContactsContract.CommonDataKinds.Phone.TYPE_MOBILE)
                    .build()
            )
        }

        for (email in emails) {
            ops.add(
                ContentProviderOperation.newInsert(ContactsContract.Data.CONTENT_URI)
                    .withValueBackReference(ContactsContract.Data.RAW_CONTACT_ID, rawContactInsertIndex)
                    .withValue(ContactsContract.Data.MIMETYPE, ContactsContract.CommonDataKinds.Email.CONTENT_ITEM_TYPE)
                    .withValue(ContactsContract.CommonDataKinds.Email.ADDRESS, email)
                    .withValue(ContactsContract.CommonDataKinds.Email.TYPE, ContactsContract.CommonDataKinds.Email.TYPE_WORK)
                    .build()
            )
        }

        try {
            context.contentResolver.applyBatch(ContactsContract.AUTHORITY, ops)
        } catch (e: Exception) {
            // Log or best effort
        }
    }

    private fun escapeVCard(str: String): String =
        str.replace("\\", "\\\\").replace(";", "\\;").replace(",", "\\,").replace("\n", "\\n")

    private fun unescapeVCard(str: String): String =
        str.replace("\\n", "\n").replace("\\,", ",").replace("\\;", ";").replace("\\\\", "\\")
}
