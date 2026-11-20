//والـ Request ممكن يحتوي على:
//Property Purpose
//Amount	قيمة المعاملةئ
//Currency	العملة
//AccountId	الحساب
//PaymentToken	Token لوسيلة الدفع
//MerchantId	التاجر
//DeviceId	الجهاز
//IPAddress	عنوان IP
//Timestamp	وقت العملية

//ملحوظة: مش بنخزن رقم الكارت الحقيقي؛ بنستخدم Token أو Hash.

//ثالثًا: Enforce Idempotency
//دي نقطة مهمة جدًا في المشروع.

//تخيل السيناريو ده:
//1.الـ External System بعت Assessment Request.
//2. منصتك قيّمت العملية ورجعت ALLOW.
//3. حصل Network Timeout قبل ما الرد يوصل للعميل.
//4. العميل بعت نفس الطلب مرة تانية.
//إحنا مش عايزين نعيد تقييم نفس العملية وننشئ Decision جديدة.

//الحل هو Idempotency.

//الـ External System يبعت Header:

//Idempotency - Key: payment - 12345

//الخدمة تحتفظ بمفتاح الطلب والنتيجة المرتبطة بيه.

//لو نفس المفتاح ونفس الـ Payload وصلوا مرة تانية، ترجع نفس النتيجة المخزنة.    

//أما لو نفس المفتاح اتبعت مع بيانات مختلفة، ترجع:

//409 Conflict

//وده موجود بالفعل في UC-04 عندك

//رابعًا: Orchestrate Detection
//كلمة Orchestration معناها إن الخدمة بتنسّق الشغل بين باقي الخدمات.

//هي مش مسؤولة عن تنفيذ قواعد الاحتيال بنفسها، لكنها بتعرف مين تستدعي وإمتى.

//مثال:
//1.تستدعي Payment Fraud عشان تفحص قواعد الدفع.
//2. تستدعي ML Scoring عشان تحصل على Risk Score (لما نضيفها).
//3. تجمع نتائج التقييم.
//4. تبعت النتائج إلى Risk Decision.
//5. تستقبل القرار النهائي.
//cd src/Osta.API
//dotnet user-secrets init
//dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Database=Osta;User Id=sa;Password=كلمة_السر_بتاعتك;TrustServerCertificate=True"



