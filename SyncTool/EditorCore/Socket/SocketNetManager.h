#ifndef _SOCKET_NET_MANAGER_H_
#define _SOCKET_NET_MANAGER_H_

#include "CommonDefine.h"

#ifdef _USE_SOCKET_UDP

#include "txMemeryTrace.h"
#include "SocketPacketFactory.h"

struct OUTPUT_STREAM
{
	OUTPUT_STREAM(char* data, int dataSize)
	:
	mData(data),
	mDataSize(dataSize)
	{}
	char* mData;
	int mDataSize;
};

struct INPUT_ELEMENT
{
	INPUT_ELEMENT(SOCKET_PACKET type, char* data, int dataSize)
	:
	mType(type),
	mData(data),
	mDataSize(dataSize)
	{}
	SOCKET_PACKET mType;
	char* mData;
	int mDataSize;
};

class SocketPacketFactoryManager;
// ʹ��socketʱ����Ϊ��������
class SocketNetManager
{
public:
	SocketNetManager();
	~SocketNetManager();
	void init(const int& port, const int& broadcastPort);
	void update(const float& elapsedTime);
	void destroy();
	void heartBeat(){}
	void processOutput(){}
	void processInput();
	SocketPacket* createPacket(const SOCKET_PACKET& type);
	void destroyPacket(SocketPacket* packet);
	void sendMessage(SocketPacket* packet, const bool& destroyPacketEndSend = true);
protected:
	static DWORD WINAPI updateUdpServer(LPVOID lpParameter);
	static DWORD WINAPI updateOutput(LPVOID lpParameter);
	void receivePacket(const SOCKET_PACKET& type, char* data, const int& dataSize)
	{
		char* p = TRACE_NEW_ARRAY(char, dataSize, p);
		memcpy(p, data, dataSize);
		INPUT_ELEMENT element(type, p, dataSize);
		// �ȴ������������Ķ�д,������������
		WaitForSingleObject(mInputMutex, INFINITE);
		mReceiverStream.push_back(element);
		// �����������Ķ�д
		ReleaseMutex(mInputMutex);
	}
	static SOCKET_PACKET checkPacketType(char* buffer, int bufferLen);
protected:
	SocketPacketFactoryManager*	mSocketPacketFactoryManager;
	txVector<OUTPUT_STREAM>		mOutputStreamList;		// �����
	float						mCurrElapsedTime;		// ��ǰ��ʱ 
	int							mPort;                  // �˿ں�
	int							mBroadcastPort;			// �㲥�Ķ˿ں�
	SOCKET						mServer;				// ���ڽ�����Ϣ���׽���
	SOCKET						mBroadcastSocket;		// ���ڹ㲥���׽���
	SOCKADDR_IN					mBroadcastAddr;			// �㲥��ַ
	txVector<INPUT_ELEMENT>		mInputStreamList;		// ������
	HANDLE						mInputMutex;			// ������д������,�������ܶ�д������
	HANDLE						mSocketThread;			// �׽����߳̾��
	HANDLE						mOutputMutex;			// �����������
	HANDLE						mOutputThread;			// ����������߳�
	txVector<INPUT_ELEMENT>		mReceiverStream;		// ���յ���
};

#endif

#endif