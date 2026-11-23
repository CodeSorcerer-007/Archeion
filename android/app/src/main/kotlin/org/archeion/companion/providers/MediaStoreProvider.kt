package org.archeion.companion.providers

import android.content.ContentUris
import android.content.Context
import android.net.Uri
import android.os.Build
import android.provider.MediaStore
import java.io.InputStream
import java.security.MessageDigest

data class MediaItem(
    val uri: Uri,
    val logicalPath: String,
    val sizeBytes: Long,
    val lastModifiedUtc: Long,
    val category: String
)

class MediaStoreProvider(private val context: Context) {

    fun queryImages(): List<MediaItem> = queryCollection(
        collectionUri = MediaStore.Images.Media.EXTERNAL_CONTENT_URI,
        category = "Photos",
        subfolder = "DCIM/Camera"
    )

    fun queryVideos(): List<MediaItem> = queryCollection(
        collectionUri = MediaStore.Video.Media.EXTERNAL_CONTENT_URI,
        category = "Videos",
        subfolder = "Movies"
    )

    fun queryAudio(): List<MediaItem> = queryCollection(
        collectionUri = MediaStore.Audio.Media.EXTERNAL_CONTENT_URI,
        category = "Audio",
        subfolder = "Music"
    )

    fun queryDocuments(): List<MediaItem> {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            return queryCollection(
                collectionUri = MediaStore.Downloads.EXTERNAL_CONTENT_URI,
                category = "Documents",
                subfolder = "Download"
            )
        }
        return emptyList()
    }

    private fun queryCollection(
        collectionUri: Uri,
        category: String,
        subfolder: String
    ): List<MediaItem> {
        val items = mutableListOf<MediaItem>()
        val projection = arrayOf(
            MediaStore.MediaColumns._ID,
            MediaStore.MediaColumns.DISPLAY_NAME,
            MediaStore.MediaColumns.SIZE,
            MediaStore.MediaColumns.DATE_MODIFIED
        )

        val cursor = context.contentResolver.query(
            collectionUri,
            projection,
            null,
            null,
            "${MediaStore.MediaColumns.DATE_MODIFIED} DESC"
        ) ?: return emptyList()

        cursor.use { c ->
            val idColumn = c.getColumnIndexOrThrow(MediaStore.MediaColumns._ID)
            val nameColumn = c.getColumnIndexOrThrow(MediaStore.MediaColumns.DISPLAY_NAME)
            val sizeColumn = c.getColumnIndexOrThrow(MediaStore.MediaColumns.SIZE)
            val dateColumn = c.getColumnIndexOrThrow(MediaStore.MediaColumns.DATE_MODIFIED)

            while (c.moveToNext()) {
                val id = c.getLong(idColumn)
                val name = c.getString(nameColumn) ?: "unknown_$id"
                val size = c.getLong(sizeColumn)
                val dateSec = c.getLong(dateColumn)
                val itemUri = ContentUris.withAppendedId(collectionUri, id)

                val sanitizedName = name.replace("[^a-zA-Z0-9._-]".toRegex(), "_")
                val logicalPath = "$subfolder/$sanitizedName"

                items.add(
                    MediaItem(
                        uri = itemUri,
                        logicalPath = logicalPath,
                        sizeBytes = size,
                        lastModifiedUtc = dateSec * 1000L,
                        category = category
                    )
                )
            }
        }
        return items
    }

    fun openInputStream(item: MediaItem): InputStream? {
        return context.contentResolver.openInputStream(item.uri)
    }

    fun computeSha256(item: MediaItem): String {
        val md = MessageDigest.getInstance("SHA-256")
        context.contentResolver.openInputStream(item.uri)?.use { input ->
            val buffer = ByteArray(81920)
            var read: Int
            while (input.read(buffer).also { read = it } != -1) {
                md.update(buffer, 0, read)
            }
        }
        return md.digest().joinToString("") { "%02x".format(it) }
    }
}
