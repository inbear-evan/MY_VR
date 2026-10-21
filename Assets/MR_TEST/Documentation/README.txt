MR 실습 프로젝트 - 공간 보기 / 책상 맞춤 / 이벤트
기준: Unity 6000.3.25f1 / Meta XR Core 207.0.0 / MRUK 207.0.0 / XRI 3.5.1
프로젝트: E:\Unity\MY_VR
폴더: Assets/MR_TEST

먼저 해보기
1. Unity Hub에서 기존 MY_VR을 6000.3.25f1로 엽니다.
2. MR 실습 > 1. 공간 예시 열기를 선택하고 Play / Game에서 공간 자산을 봅니다.
   MR_RoomPreview는 공식 MRUK Office00 방 프리팹을 보는 준비된 씬입니다.
   PC에서 보이는 방은 실제 PC 주변 공간을 촬영하거나 인식한 결과가 아닙니다.
3. Play를 끄고 MR 실습 > 2. 책상과 이벤트 열기를 선택합니다.
   또는 Assets > MR_TEST > Scenes > MR_TableEvent를 엽니다.
4. Play / Game에서 '모의 책상에 단상 맞추기'를 누릅니다.
   모의 책상 가로 1.60m, 깊이 0.90m, 상판 높이 0.75m에 단상이 맞춰집니다.
5. '빨간 상자 올리기'를 누릅니다. 잘못된 상자로는 문이 열리지 않습니다.
6. '초록 상자 올리기'를 누릅니다. 흰 받침대 위에서 상자가 안정되면 문이 열립니다.
7. '처음 상태로'를 누르면 소품과 문, 성공 상태가 초기화됩니다.

PC 연습 프로그램
Builds/MR_PC/MR_Practice.exe로 같은 모의 흐름을 연습할 수 있습니다.
공간 보기 실행은 함께 제공된 StartRoomPreview.cmd를 사용합니다.
PC 버튼으로 올리는 동작은 Quest 컨트롤러의 실제 잡기를 대신하는 모의 입력입니다.

Quest에서 이어서 하기 - 아직 기기 검증 전
1. 기기 안의 공간 설정에서 방과 책상(TABLE)을 등록합니다.
   등록된 공간 모델을 읽어 단상을 맞추며 움직이는 실물 책상을 추적하지 않습니다.
2. MR 실습 > 4. Quest 공간·패스스루 설정 준비를 선택합니다.
   이 메뉴는 공통 Meta Project Config의 Scene/Passthrough Support를 Supported로 설정합니다.
   PC 실행이나 씬 생성만 할 때는 이 메뉴를 자동으로 실행하지 않습니다.
3. Meta의 Project Setup Tool에서 현재 씬과 Android OpenXR/Meta XR Feature 설정을 확인합니다.
4. Android Build Profiles의 Scene List에 MR_TableEvent를 추가·체크하고 다른 실습 씬은 해제합니다.
5. Quest의 공간 정보 접근 권한을 허용합니다. 등록된 방의 TABLE 중 가장 큰 유효 책상을 사용합니다.
   책상의 상판 중심, 가로·깊이와 방향을 사용합니다. 실제 등록 크기가 틀리면 공간 설정을 수정합니다.
6. 컨트롤러 그립으로 가상 초록 상자를 잡고 흰 받침대 위에서 놓습니다.
   잡고 있는 동안, 받침대 밖, 공중에 떠 있는 상태, 정답이 아닌 상자로는 완료되지 않습니다.
7. 오른손 A는 초기화, B는 공간 다시 읽기입니다.
8. 책상을 찾지 못하면 실제 모드에서는 가짜 책상으로 대신 완료하지 않습니다.
   TABLE 등록과 공간 접근 권한을 확인하고 다시 읽습니다.

이번 프로젝트의 문은 이벤트 결과를 보여주는 가상 문입니다.
실제 문을 조작하거나 실물 상자를 자동 인식하는 기능은 포함하지 않습니다.
실물 책상 위치·크기에 단상을 맞추는 과정과 가상 소품의 조건 이벤트가 목표입니다.
Quest 2/3의 공간 설정·패스스루 동작은 기기 연결 후 별도로 확인해야 합니다.
자동 3D 공간 메시나 실시간 환경 깊이는 이번 기본 기능의 필수 조건이 아닙니다.

폴더
Scenes: MR_RoomPreview / MR_TableEvent
Scripts: 책상 형상, 가상 소품, 실습 상태와 이벤트, PC 검증
Editor: 씬 생성·열기, PC 빌드, Quest 설정 메뉴
Materials / Settings / Fonts: 실습용 재질·URP 설정·한글 글꼴
Documentation: 안내와 검증 기록

참고
https://developers.meta.com/vr/documentation/unity/unity-mr-utility-kit-gs/
https://developers.meta.com/vr/documentation/unity/unity-mr-utility-kit-manage-scene-data/
https://developers.meta.com/vr/documentation/unity/unity-mr-utility-kit-samples/

기존 VR / AR / Session3_DoorMeet 씬은 이번 실습 씬과 구분합니다.
한글 글꼴의 OFL.txt를 함께 보존합니다.
