# ConfigReader
Unity配置表读取类，配置表格式使用excel编写到处txt格式

代码并不成熟，还望见谅



工具用法：
在游戏最开始时调用Init函数，初始化ConfigReader中的Config字典，调用其中相应的读取函数获取Config

注意：


1.Config保存位置为Resources下的Config文件夹，命名规则严格按照***Config.txt


2.Config格式：前三行分别为变量类型、变量中文名、变量名。特别的，枚举类型类型名为enum，变量名为x**  （x代表首字母小写），在对应Model类中的变量名严格为***Enum。


3.Config编写：使用Excel按照上述要求编写，完成后另存为txt，使用UTF-8编码（不使用未测试），最好检查txt最后是否用空行（有不会影响，但会报警告）。


4.Model类编写：不继承monobehavior；类名严格为***Config，即和配置表txt名字一样；变量个数、变量类型、变量命名严格符合要求。



工具类使用反射机制

作者：
倪宏宇 2020-07-17
