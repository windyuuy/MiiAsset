package com.unity3d.myutils;

import java.util.ArrayList;
import java.util.LinkedList;
import java.util.List;
import java.util.Queue;

public class ForwardConsumerStream {
    int BufferSize = 4096 * 8;
    protected int start;
    public int end;
    protected int pos;
    protected List<byte[]> bufferList = new ArrayList<>();

    public class ReadUnit {
        public int start;
        public int end;
    }

    protected Queue<ReadUnit> readList = new LinkedList<ReadUnit>();

    public void seek(int pos) {
        if (pos < 0 || pos > end) {
//            throw new Exception("jf overflow1");
        }
        this.pos = pos;
    }

    public void read(byte[] array, int offset, int count) {
        if (pos + count > end) {
//            throw new Exception("jf overflow2");
        }
        int copyEnd = pos + count;

        int copyStart = pos;
        while (copyStart < end) {
            int aIndex = getArrayIndex(copyStart);
            int id = getPieceIndex(copyStart);

            int idEnd = getPieceEndIndex(aIndex, copyEnd);
            int copyLen = idEnd - id;
            byte[] targetArray = bufferList.get(aIndex);
            System.arraycopy(targetArray, id, array, offset, copyLen);

            copyStart += copyLen;
        }
        pos = copyStart;
    }

    public void put(byte[] array, int pos, int len) {
        int copyEnd = pos + len;
        int endIndex = bufferList.size();

        int aIndex = getArrayIndex(pos);

        for (int i = endIndex; i <= aIndex; i++) {
            bufferList.add(null);
        }

        int copyStart = pos;
        while (copyStart < copyEnd) {
            aIndex = getArrayIndex(copyStart);
            int id = getPieceIndex(pos);
            int idEnd = getPieceEndIndex(aIndex, copyEnd);
            int copyLen = idEnd - id;

            byte[] buffer = bufferList.get(aIndex);
            System.arraycopy(array, pos, buffer, id, copyLen);
            copyStart += copyLen;
        }

    }

    protected int getArrayIndex(int pos) {

        return pos;
    }

    protected int getPieceIndex(int pos) {

        return pos;
    }

    protected int getPieceEndIndex(int aIndex, int copyEnd) {
        return 0;
    }

    protected void dropRead() {

    }
}
