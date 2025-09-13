package com.unity3d.myutils;

import android.app.Activity;
import android.content.res.AssetManager;

import java.io.BufferedInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.security.InvalidParameterException;
import java.util.ArrayList;
import java.util.LinkedList;
import java.util.List;
import java.util.Queue;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import java.util.stream.Stream;

public class JarFileStream {
    InputStream inputStream;

    protected long pos;
    protected long mMarkPos;

    public long GetLength() {
        try {
            return inputStream.available();
        } catch (IOException e) {
            e.printStackTrace();
            return -1;
        }
    }

    public long GetPosition() {
        return pos;
    }

    public boolean Open(String path) {
        // 创建 Pattern 对象
        Pattern r = Pattern.compile("base\\.apk!/assets/(.+)");
        // 现在创建 matcher 对象
        Matcher m = r.matcher(path);
        String subPath = m.group(1);
        Activity activity = com.unity3d.player.UnityPlayer.currentActivity;
        AssetManager assetManager = activity.getAssets();
        try {
            assert subPath != null;
            inputStream = assetManager.open(subPath);
            mMarkPos = 0;
            assert inputStream.markSupported();
            // 2**31-1
            inputStream.mark(2147483647);
            return true;
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    }


    /**
     * @param offset 0-begin,current,end
     * @param origin
     * @return
     */
    public long Seek(long offset, int origin) {
        long skip;
        if (origin == 0) {
            skip = offset - pos;
        } else if (origin == 1) {
            skip = offset;
        } else if (origin == 2) {
            try {
                skip = inputStream.available() - offset - pos;
            } catch (IOException e) {
                e.printStackTrace();
                return pos;
            }
        } else {
            throw new InvalidParameterException("origin-cannot-be-" + origin);
        }

        try {
            if (skip < 0) {
                skip = pos + skip;
                inputStream.reset();
                pos = mMarkPos;
            }

            long skip1 = inputStream.skip(skip);
            pos += skip1;
        } catch (IOException e) {
            e.printStackTrace();
            return pos;
        }
        return pos;
    }

    public int Read(byte[] array, int offset, int count) {
        int count1 = 0;
        try {
            count1 = inputStream.read(array, offset, count);
        } catch (IOException e) {
            e.printStackTrace();
            return -1;
        }
        return count1;
    }

    public boolean Close() {
        try {
            inputStream.close();
            return true;
        } catch (IOException e) {
            e.printStackTrace();
            return false;
        }
    }

    public void Dispose() {
    }

}
